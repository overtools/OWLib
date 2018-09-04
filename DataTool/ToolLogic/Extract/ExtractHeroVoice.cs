using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using DataTool.DataModels;
using DataTool.SaveLogic.Unlock;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.ToolLogic.Extract
{
    [Tool("extract-hero-voice", Description = "Extract hero voice sounds", TrackTypes = new ushort[] { 0x75 }, CustomFlags = typeof(ExtractFlags))]
    public class ExtractHeroVoice : QueryParser, ITool, IQueryParser {
        public List<QueryType> QueryTypes => new List<QueryType> { new QueryType { Name = "soundRestriction" }, new QueryType { Name = "groupRestriction" } };

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
            Log($"{indent + 1}Command format: \"{{hero name}}|{{type}}=({{tag name}}={{tag}}),{{item name}}\"");
            Log($"{indent + 1}Each query should be surrounded by \", and individual queries should be separated by spaces");

            Log($"{indent + 1}All hero names are in your selected locale");

            Log("\r\nTypes:");
            foreach (QueryType argType in types) {
                Log($"{indent + 1}{argType.Name}");
            }

            Log("\r\nExample commands: ");
            Log($"{indent + 1}\"Lúcio|soundRestriction=00000000B56B.0B2\"");
            Log($"{indent + 1}\"Torbjörn|groupRestriction=0000000000CD.078\"");
            Log($"{indent + 1}\"Moira\"");
        }

        private const string Container = "HeroVoice";

        private void SaveHeroSounds(ICLIFlags toolFlags) {
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
                STUHero hero = GetInstance<STUHero>(heroFile);
                if (hero == null) continue;

                string heroNameActual = (GetString(hero.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(heroFile)}").TrimEnd(' ');


                Dictionary<string, ParsedArg> config = GetQuery(parsedTypes, heroNameActual.ToLowerInvariant(), "*");

                if (config.Count == 0) continue;
                
                Log($"Processing data for {heroNameActual}");
                
                STUVoiceSetComponent baseComponent = default;
                Combo.ComboInfo baseInfo = default;

                string heroFileName = GetValidFilename(heroNameActual);

                if (SaveSet(flags, basePath, hero.m_gameplayEntity, heroFileName, "Default", ref baseComponent, ref baseInfo)) {
                    if (hero.m_heroProgression == 0) continue;
                    var progression = new ProgressionUnlocks(hero);

                    bool npc = progression.LootBoxesUnlocks == null;

                    foreach (Unlock itemInfo in progression.OtherUnlocks) {
                        ProcessUnlock(itemInfo, flags, basePath, heroFileName, hero, baseComponent, baseInfo);
                    }

                    if (npc) {
                        foreach (var skin in hero.m_skinThemes)
                            SaveSkin(flags, skin.m_5E9665E3, basePath, hero, heroFileName, GetFileName(skin.m_5E9665E3), baseComponent, baseInfo);

                        continue;
                    }

                    foreach (var defaultUnlocks in progression.LevelUnlocks) {
                        foreach (Unlock unlock in defaultUnlocks.Unlocks) {
                            ProcessUnlock(unlock, flags, basePath, heroFileName, hero, baseComponent, baseInfo);
                        }
                    }

                    foreach (var eventUnlocks in progression.LootBoxesUnlocks) {
                        if (eventUnlocks?.Unlocks == null) continue;

                        foreach (Unlock unlock in eventUnlocks.Unlocks) {
                            ProcessUnlock(unlock, flags, basePath, heroFileName, hero, baseComponent, baseInfo);
                        }
                    }
                }
            }
        }

        private static void ProcessUnlock(Unlock unlock, ICLIFlags flags, string basePath, string heroFileName, STUHero hero, STUVoiceSetComponent baseComponent, Combo.ComboInfo baseInfo) {
            if (!(unlock.STU is STUUnlock_SkinTheme unlockSkinTheme)) return;
            if (unlockSkinTheme.m_0B1BA7C1 != 0)
                return;

            SaveSkin(flags, unlockSkinTheme.m_skinTheme, basePath, hero, heroFileName, unlock.Name, baseComponent, baseInfo);
        }

        private static void SaveSkin(ICLIFlags flags, ulong skinResource, string basePath, STUHero hero, string heroFileName, string name, STUVoiceSetComponent baseComponent, Combo.ComboInfo baseInfo) {
            STUSkinTheme skin = GetInstance<STUSkinTheme>(skinResource);
            if (skin == null)
                return;

            STUVoiceSetComponent component = default;
            Combo.ComboInfo info = default;

            SaveSet(flags, basePath, hero.m_gameplayEntity, heroFileName, GetValidFilename(name), ref component,
                ref info, baseComponent, baseInfo, SkinTheme.GetReplacements(skin));
        }

        private static bool SaveSet(ICLIFlags flags, string basePath, ulong entityMain, string heroFileName, string skin, ref STUVoiceSetComponent voiceSetComponent, ref Combo.ComboInfo info, STUVoiceSetComponent baseComponent = null, Combo.ComboInfo baseCombo = null, Dictionary<ulong, ulong> replacements = null) {
            voiceSetComponent = GetInstance<STUVoiceSetComponent>(Combo.GetReplacement(entityMain, replacements));

            if (voiceSetComponent?.m_voiceDefinition == null) {
                Debugger.Log(0, "DataTool.SaveLogic.Unlock.VoiceLine", "[DataTool.SaveLogic.Unlock.VoiceLine]: VoiceSet not found");
                return false;
            }

            info = new Combo.ComboInfo();
            Combo.Find(info, Combo.GetReplacement(voiceSetComponent.m_voiceDefinition, replacements), replacements);
            if (baseComponent != null && baseCombo != null) {
                if (!Combo.RemoveDuplicateVoiceSetEntries(baseCombo, ref info, baseComponent.m_voiceDefinition, Combo.GetReplacement(voiceSetComponent.m_voiceDefinition, replacements)))
                    return false;
            }

            Log($"\tSaving {skin}");

            SaveLogic.Combo.SaveVoiceSet(flags, Path.Combine(basePath, Container, heroFileName, skin), info, Combo.GetReplacement(voiceSetComponent.m_voiceDefinition, replacements));

            return true;
        }
    }
}