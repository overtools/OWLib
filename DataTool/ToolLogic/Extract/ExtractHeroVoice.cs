using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using OWLib;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using System.Linq;
using DataTool.DataModels;

namespace DataTool.ToolLogic.Extract
{
    [Tool("extract-hero-voice", Description = "Extract hero voice sounds", TrackTypes = new ushort[] { 0x75 }, CustomFlags = typeof(ExtractFlags))]
    public class ExtractHeroVoice : QueryParser, ITool, IQueryParser
    {
        public List<QueryType> QueryTypes => new List<QueryType> { new QueryType { Name = "soundRestriction" }, new QueryType { Name = "groupRestriction" } };

        public Dictionary<string, string> QueryNameOverrides => ExtractHeroUnlocks.HeroMapping;

        public void IntegrateView(object sender)
        {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags)
        {
            SaveHeroSounds(toolFlags);
        }

        protected override void QueryHelp(List<QueryType> types)
        {
            IndentHelper indent = new IndentHelper();

            Log("Please specify what you want to extract:");
            Log($"{indent + 1}Command format: \"{{hero name}}|{{type}}=({{tag name}}={{tag}}),{{item name}}\"");
            Log($"{indent + 1}Each query should be surrounded by \", and individual queries should be seperated by spaces");

            Log($"{indent + 1}All hero names are in your selected locale");

            Log("\r\nTypes:");
            foreach (QueryType argType in types)
            {
                Log($"{indent + 1}{argType.Name}");
            }

            Log("\r\nExample commands: ");
            Log($"{indent + 1}\"Lúcio|soundRestriction=00000000B56B.0B2\"");
            Log($"{indent + 1}\"Torbjörn|groupRestriction=0000000000CD.078\"");
            Log($"{indent + 1}\"Moira\"");
        }

        private static string Container = "HeroVoice";

        public void SaveHeroSounds(ICLIFlags toolFlags)
        {
            string basePath;
            if (toolFlags is ExtractFlags flags)
            {
                basePath = flags.OutputPath;
            }
            else
            {
                throw new Exception("no output path");
            }

            if (flags.Positionals.Length < 4)
            {
                QueryHelp(QueryTypes);
                return;
            }

            Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes = ParseQuery(flags, QueryTypes, QueryNameOverrides);
            if (parsedTypes == null) return;

            foreach (ulong heroFile in TrackedFiles[0x75])
            {
                STUHero hero = GetInstance<STUHero>(heroFile);
                if (hero == null) continue;

                string heroNameActual = (GetString(hero.Name) ?? $"Unknown{GUID.Index(heroFile)}").TrimEnd(' ');
                
                Dictionary<string, ParsedArg> config = new Dictionary<string, ParsedArg>();
                foreach (string key in new[] { heroNameActual.ToLowerInvariant(), "*" })
                {
                    if (!parsedTypes.ContainsKey(key)) continue;
                    
                    Log($"Processing data for {heroNameActual}");
                    foreach (KeyValuePair<string, ParsedArg> parsedArg in parsedTypes[key])
                    {
                        if (config.ContainsKey(parsedArg.Key))
                        {
                            config[parsedArg.Key] = config[parsedArg.Key].Combine(parsedArg.Value);
                        }
                        else
                        {
                            config[parsedArg.Key] = parsedArg.Value.Combine(null); // clone for safety
                        }
                    }
                }

                if (config.Count == 0) continue;
                
                STUVoiceSetComponent baseComponent = default(STUVoiceSetComponent);
                Combo.ComboInfo baseInfo = default(Combo.ComboInfo);

                string heroFileName = GetValidFilename(heroNameActual);

                if (SaveSet(flags, basePath, hero.EntityMain, heroFileName, "Default", ref baseComponent, ref baseInfo))
                {
                    var unlocks = GetInstance<STUHeroUnlocks>(hero.LootboxUnlocks);
                    if (unlocks == null)
                    {
                        continue;
                    }

                    bool npc = unlocks.LootboxUnlocks == null;

                    var achievementUnlocks = GatherUnlocks(unlocks?.SystemUnlocks?.Unlocks?.Select(it => (ulong)it)).Where(item => item?.Unlock is STUUnlock_Skin).ToList();
                    foreach (ItemInfo itemInfo in achievementUnlocks)
                    {
                        if (itemInfo == null)
                        {
                            continue;
                        }

                        if ((itemInfo.Unlock as STUUnlock_Skin)?.LeagueTeam != 0)
                        {
                            continue;
                        }

                        SaveSkin(flags, (itemInfo.Unlock as STUUnlock_Skin)?.SkinResource, basePath, hero, heroFileName, itemInfo.Name, baseComponent, baseInfo);
                    }

                    if (npc)
                    {
                        foreach (STUHeroSkin skin in hero.Skins)
                        {
                            SaveSkin(flags, skin.SkinOverride, basePath, hero, heroFileName, GetFileName(skin.SkinOverride), baseComponent, baseInfo);
                        }
                        continue;
                    }

                    // todo: not this
                    foreach (var defaultUnlocks in unlocks.TankLibPlease)
                    {
                        var dUnlocks = GatherUnlocks(defaultUnlocks.m_unlocks.Unlocks.Select(it => (ulong)it)).Where(item => item?.Unlock is STUUnlock_Skin).ToList();

                        foreach (ItemInfo itemInfo in dUnlocks)
                        {
                            if (itemInfo == null)
                                continue;

                            if ((itemInfo.Unlock as STUUnlock_Skin)?.LeagueTeam != 0)
                                continue;

                            SaveSkin(flags, (itemInfo.Unlock as STUUnlock_Skin)?.SkinResource, basePath, hero, heroFileName, itemInfo.Name, baseComponent, baseInfo);
                        }
                    }

                    foreach (var eventUnlocks in unlocks.LootboxUnlocks)
                    {
                        if (eventUnlocks?.Unlocks?.Unlocks == null) continue;

                        var eUnlocks = eventUnlocks.Unlocks.Unlocks.Select(it => GatherUnlock(it)).Where(item => item?.Unlock is STUUnlock_Skin).ToList();
                        foreach (ItemInfo itemInfo in eUnlocks)
                        {
                            if (itemInfo == null)
                            {
                                continue;
                            }

                            if ((itemInfo.Unlock as STUUnlock_Skin)?.LeagueTeam != 0)
                            {
                                continue;
                            }

                            SaveSkin(flags, (itemInfo.Unlock as STUUnlock_Skin)?.SkinResource, basePath, hero, heroFileName, itemInfo.Name, baseComponent, baseInfo);
                        }
                    }
                }
            }
        }

        public static void SaveSkin(ICLIFlags flags, ulong skinResource, string basePath, STUHero hero, string heroFileName, string name, STUVoiceSetComponent baseComponent, Combo.ComboInfo baseInfo)
        {
            STUSkinOverride skin = GetInstance<STUSkinOverride>(skinResource);
            if (skin == null)
            {
                return;
            }

            STUVoiceSetComponent component = default(STUVoiceSetComponent);
            Combo.ComboInfo info = default(Combo.ComboInfo);

            if (SaveSet(flags, basePath, hero.EntityMain, heroFileName, GetValidFilename(name), ref component, ref info, baseComponent, baseInfo, skin.ProperReplacements))
            {
                return;
            }
        }

        public static bool SaveSet(ICLIFlags flags, string basePath, ulong entityMain, string heroFileName, string skin, ref STUVoiceSetComponent soundSetComponentContainer, ref Combo.ComboInfo info, STUVoiceSetComponent baseComponent = null, Combo.ComboInfo baseCombo = null, Dictionary<ulong, ulong> replacements = null)
        {
            soundSetComponentContainer = GetInstance<STUVoiceSetComponent>(Combo.GetReplacement(entityMain, replacements));

            if (soundSetComponentContainer?.VoiceSet == null)
            {
                Debugger.Log(0, "DataTool.SaveLogic.Unlock.VoiceLine", "[DataTool.SaveLogic.Unlock.VoiceLine]: VoiceSet not found");
                return false;
            }

            info = new Combo.ComboInfo();
            Combo.Find(info, Combo.GetReplacement(soundSetComponentContainer.VoiceSet, replacements), replacements);
            if (baseComponent != null && baseCombo != null)
            {
                if (!Combo.RemoveDuplicateVoiceSetEntries(baseCombo, ref info, baseComponent.VoiceSet, Combo.GetReplacement(soundSetComponentContainer.VoiceSet, replacements)))
                {
                    return false;
                }
            }

            Log("Saving {0}", skin);

            SaveLogic.Combo.SaveVoiceSet(flags, Path.Combine(basePath, Container, heroFileName, skin), info, Combo.GetReplacement(soundSetComponentContainer.VoiceSet, replacements));

            return true;
        }
    }
}
