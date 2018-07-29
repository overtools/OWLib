using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-hero-voice", Description = "Extract hero voice sounds", TrackTypes = new ushort[] {0x75}, CustomFlags = typeof(ExtractFlags))]
    public class ExtractHeroVoice : QueryParser, ITool, IQueryParser {
        public List<QueryType> QueryTypes => new List<QueryType> {new QueryType {Name = "soundRestriction"}, new QueryType {Name = "groupRestriction"}};

        public Dictionary<string, string> QueryNameOverrides => ExtractHeroUnlocks.HeroMapping;

        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            SaveHeroSounds(toolFlags);
        }
        
        protected override void QueryHelp(List<QueryType> types) {
            IndentHelper indent = new IndentHelper();
            
            Log("Please specify what you want to extract:");
            Log($"{indent+1}Command format: \"{{hero name}}|{{type}}=({{tag name}}={{tag}}),{{item name}}\"");
            Log($"{indent+1}Each query should be surrounded by \", and individual queries should be seperated by spaces");
            
            Log($"{indent+1}All hero names are in your selected locale");
                        
            Log("\r\nTypes:");
            foreach (QueryType argType in types) {
                Log($"{indent+1}{argType.Name}");
            }
            
            Log("\r\nExample commands: ");
            Log($"{indent+1}\"Lúcio|soundRestriction=00000000B56B.0B2\"");
            Log($"{indent+1}\"Torbjörn|groupRestriction=0000000000CD.078\"");
            Log($"{indent+1}\"Moira\"");
        }

        private static string Container = "HeroVoice";

        public void SaveHeroSounds(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            
            if (flags.Positionals.Length < 4) {
                QueryHelp(QueryTypes);
                return;
            }

            Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes = ParseQuery(flags, QueryTypes, QueryNameOverrides);
            if (parsedTypes == null) return;
            
            foreach (ulong heroFile in TrackedFiles[0x75]) {
                STUHero hero = GetInstanceNew<STUHero>(heroFile);
                if (hero == null) continue;

                string heroNameActual = (GetString(hero.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(heroFile)}").TrimEnd(' ');

                Dictionary<string, ParsedArg> config = GetQuery(parsedTypes, heroNameActual.ToLowerInvariant(), "*");
                
                if (config.Count == 0) continue;
                
                Log($"Processing data for {heroNameActual}");
                
                STUVoiceSetComponent voiceSetComponent = GetInstanceNew<STUVoiceSetComponent>(hero.m_gameplayEntity);

                if (voiceSetComponent?.m_voiceDefinition == null) {
                    Debugger.Log(0, "DataTool.SaveLogic.Unlock.VoiceLine", "[DataTool.SaveLogic.Unlock.VoiceLine]: VoiceSet not found");
                    return;
                }
                
                string heroFileName = GetValidFilename(heroNameActual);
                
                Combo.ComboInfo info = new Combo.ComboInfo();
                Combo.Find(info, voiceSetComponent.m_voiceDefinition);
                
                SaveLogic.Combo.SaveVoiceSet(flags, Path.Combine(basePath, Container, heroFileName), info, voiceSetComponent.m_voiceDefinition);
            }
        }
    }
}