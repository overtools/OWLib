using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.SaveLogic.Unlock;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-hero-convo", Description = "Extract hero voice conversations", TrackTypes = new ushort[] {0x75}, CustomFlags = typeof(ExtractFlags))]
    public class ExtractHeroConversations : QueryParser, ITool, IQueryParser {
        public List<QueryType> QueryTypes => new List<QueryType> {new QueryType {Name = "FakeType"}};

        public Dictionary<string, string> QueryNameOverrides => ExtractHeroUnlocks.HeroMapping;

        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ExtractHeroConvos(toolFlags);
        }
        
        protected override void QueryHelp(List<QueryType> types) {
            IndentHelper indent = new IndentHelper();
            
            Log("Please specify what you want to extract:");
            Log($"{indent+1}Command format: \"{{hero name}}|{{type}}=({{tag name}}={{tag}}),{{item name}}\"");
            Log($"{indent+1}Each query should be surrounded by \", and individual queries should be seperated by spaces");
            
            Log($"{indent+1}All hero names are in your selected locale");
            
            Log("\r\nExample commands: ");
            Log($"{indent+1}\"Lúcio\"");
            Log($"{indent+1}\"Torbjörn\"");
            Log($"{indent+1}\"Moira\"");
            Log($"{indent+1}\"Wrecking Ball\"");
        }

        private const string Container = "HeroConvo";

        public void ExtractHeroConvos(ICLIFlags toolFlags) {
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

            string path = Path.Combine(basePath, Container);

            Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes = ParseQuery(flags, QueryTypes, QueryNameOverrides);
            if (parsedTypes == null) return;

            Dictionary<ulong, VoiceSet> heroVoiceSets = new Dictionary<ulong, VoiceSet>();
            Dictionary<ulong, STUHero> heroes = new Dictionary<ulong, STUHero>();
            
            foreach (ulong heroGuid in Program.TrackedFiles[0x75]) {
                STUHero hero = GetInstanceNew<STUHero>(heroGuid);
                if (hero == null) continue;
                STUVoiceSetComponent voiceSetComponent = GetInstanceNew<STUVoiceSetComponent>(hero.m_gameplayEntity);

                if (voiceSetComponent?.m_voiceDefinition == null) {
                    Debugger.Log(0, "DataTool.SaveLogic.Unlock.VoiceLine", "[DataTool.SaveLogic.Unlock.VoiceLine]: VoiceSet not found");
                    continue;
                }
                STUVoiceSet set = GetInstanceNew<STUVoiceSet>(voiceSetComponent.m_voiceDefinition);
                if (set == null) continue;

                heroVoiceSets[heroGuid] = new VoiceSet(set);
                heroes[heroGuid] = hero;
            }

            Combo.ComboInfo comboInfo = new Combo.ComboInfo();

            foreach (ulong heroGuid in Program.TrackedFiles[0x75]) {
                if (!heroes.ContainsKey(heroGuid)) continue;
                STUHero hero = heroes[heroGuid];

                string heroNameActual = (GetString(hero.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(heroGuid)}").TrimEnd(' ');

                Dictionary<string, ParsedArg> config = GetQuery(parsedTypes, heroNameActual.ToLowerInvariant(), "*");
                
                if (config.Count == 0) continue;
                Log($"Processing data for {heroNameActual}");
                heroNameActual = GetValidFilename(heroNameActual);

                VoiceSet set = heroVoiceSets[heroGuid];

                foreach (VoiceLineInstance lineInstance in set.VoiceLines.Values) {
                    if (lineInstance.VoiceConversation == 0) continue;
                    STUVoiceConversation conversation = GetInstanceNew<STUVoiceConversation>(lineInstance.VoiceConversation);
                        
                    string convoDir = Path.Combine(path, heroNameActual, GetFileName(lineInstance.VoiceConversation));
                    foreach (STUVoiceConversationLine line in conversation.m_voiceConversationLine) {
                        string linePath = Path.Combine(convoDir, line.m_B4D405A1.ToString());
                        foreach (KeyValuePair<ulong, VoiceSet> voiceSet in heroVoiceSets) {
                            if (voiceSet.Value.VoiceLines.ContainsKey(line.m_lineGUID)) {
                                VoiceLine.SaveVoiceLine(flags, lineInstance, linePath, comboInfo);
                            }
                        }
                    }
                }
            }
        }
    }
}