using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.List;
using DataTool.ToolLogic.Util;
using TACTLib.Container;
using TankLib;
using TankLib.Helpers;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;
using SkinTheme = DataTool.SaveLogic.Unlock.SkinTheme;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-hero-voice", Description = "Extracts hero voicelines.", CustomFlags = typeof(ExtractFlags))]
    class ExtractHeroVoice : ExtractHeroVoiceBetter {
        protected override string Container => "HeroVoice";
    }

    [Tool("extract-hero-voice-better", IsSensitive = true, Description = "Extracts hero voicelines but groups them a bit better.", CustomFlags = typeof(ExtractFlags))]
    class ExtractHeroVoiceBetter : QueryParser, ITool, IQueryParser {
        public List<QueryType> QueryTypes => new List<QueryType>();
        public Dictionary<string, string> QueryNameOverrides => null;
        public string DynamicChoicesKey => UtilDynamicChoices.VALID_HERO_NAMES;
        protected virtual string Container => "BetterHeroVoice";
        private static readonly HashSet<ulong> SoundIdCache = new HashSet<ulong>();

        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            flags.EnsureOutputDirectory();
            var outputPath = Path.Combine(flags.OutputPath, Container);

            // Do normal heroes first then NPCs, this is because NPCs have a lot of duplicate sounds and normal heroes (should) have none
            // so any duplicate sounds would only come up while processing NPCs which can be ignored as they (probably) belong to heroes
            var heroesDict = Helpers.GetHeroes();
            var heroes = heroesDict.Values
                .OrderBy(x => !x.IsHero) // sort by hero first
                .ThenBy(x => x.GUID.GUID) // then by GUID
                .ToArray();

            var validHeroes = Helpers.GetHeroNamesMapping(heroesDict);
            var parsedTypes = ParseQuery(flags, QueryTypes, namesForThisLocale: validHeroes);

            var criteriaContext = CreateCriteriaContext();

            foreach (var hero in heroes) {
                // if we have a query, check if we should process this hero
                if (parsedTypes != null) {
                    var config = GetQuery(parsedTypes, hero.Name?.ToLowerInvariant(), "*", teResourceGUID.Index(hero.GUID).ToString("X"));
                    if (config.Count == 0) {
                        continue;
                    }
                }

                string heroName = GetValidFilename(GetCleanString(hero.STU.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(hero.GUID)}");

                Logger.Log($"Processing {heroName}");

                var heroVoiceSetGuid = GetInstance<STUVoiceSetComponent>(hero.STU.m_gameplayEntity)?.m_voiceDefinition;

                Combo.ComboInfo baseInfo = new Combo.ComboInfo();
                var baseContext = new SaveSetContext {
                    m_flags = flags,
                    m_basePath = outputPath,
                    m_heroName = heroName,
                    m_unlockName = "Default",
                    m_voiceSetGUID = heroVoiceSetGuid,
                    m_info = baseInfo,
                    m_criteriaContext = criteriaContext
                };

                if (!SaveVoiceSet(baseContext)) {
                    // unknown failure
                    continue;
                }
                
                var skinUnlocks = new ProgressionUnlocks(hero.STU).GetUnlocksOfType(UnlockType.Skin);

                foreach (var unlock in skinUnlocks) {
                    var unlockSkinTheme = unlock.STU as STUUnlock_SkinTheme;
                    if (unlockSkinTheme?.m_0B1BA7C1 != 0) {
                        // skip esports skins
                        // they definitely won't have skin overrides
                        continue;
                    }

                    Logger.Debug("Tool", $"Processing skin {unlock.GetName()}");
                    var skinThemeGUID = unlockSkinTheme.m_skinTheme;
                    var skinTheme = GetInstance<STUSkinBase>(skinThemeGUID);
                    if (skinTheme == null) {
                        continue;
                    }

                    Combo.ComboInfo info = new Combo.ComboInfo();
                    var skinContext = baseContext with {
                        m_info = info,
                        m_baseInfo = baseContext.m_info,
                        m_unlockName = GetValidFilename(unlock.GetName()),
                        m_replacements = SkinTheme.GetReplacements(skinThemeGUID)
                    };
                    SaveVoiceSet(skinContext);
                }
            }
        }
        
        private static CriteriaContext CreateCriteriaContext() {
            var ctx = new CriteriaContext();
            
            var heroes = Helpers.GetHeroes();
            foreach (var hero in heroes) {
                ctx.m_heroes.Add(hero.Key, hero.Value.Name);
            }

            var maps = ListMaps.GetMaps();
            foreach (var map in maps) {
                ctx.m_maps.Add(map.Value.MapGUID, map.Value.GetName());
            }

            return ctx;
        }

        public class CriteriaContext {
            public Dictionary<ulong, string> m_heroes = [];
            public Dictionary<ulong, string> m_maps = [];
            public Dictionary<ulong, string> m_gamemodes = [];
            public Dictionary<ulong, string> m_missions = [];

            public string GetHeroName(ulong guid) {
                if (m_heroes.TryGetValue(guid, out var name)) {
                    return name;
                }
                return GetNoTypeGUIDName(guid);
            }
            
            public string GetMapName(ulong guid) {
                if (m_maps.TryGetValue(guid, out var name)) {
                    return name;
                }
                return GetNoTypeGUIDName(guid);
            }

            public string GetGameModeName(ulong guid) {
                // todo: cache on startup
                var gameMode = GetInstance<STUGameMode>(guid);
                
                var gameModeName = GetCleanString(gameMode?.m_displayName);
                if (gameModeName != null) return gameModeName;
                
                return GetNoTypeGUIDName(guid);
            }

            public string GetMissionName(ulong guid) {
                // todo: cache on startup
                var mission = GetInstance<STU_8B0E97DC>(guid);
                
                var missionName = GetCleanString(mission?.m_0EDCE350);
                if (missionName != null) return missionName;
                
                return GetNoTypeGUIDName(guid);
            }

            public string GetObjectiveName(ulong guid) {
                var objective = GetInstance<STU_19A98AF4>(guid);
                
                // todo: these are not the user-facing names...
                var objectiveName = GetCleanString(objective?.m_name);
                if (objectiveName != null) return objectiveName;
                
                return GetNoTypeGUIDName(guid);
            }
            
            public string GetCelebrationName(ulong guid) 
            {
                return GetNoTypeGUIDName(guid);
            }

            public string GetTagName(ulong guid) 
            {
                return GetNoTypeGUIDName(guid);
            }

            private static string GetNoTypeGUIDName(ulong guid, string specifier="") {
                var manualName = GetNullableGUIDName(guid);
                if (manualName != null) return manualName;

                return $"Unknown{specifier}{teResourceGUID.Index(guid):X}";
            }
        }

        public struct SaveSetContext {
            public required ExtractFlags m_flags;
            public required string m_basePath;
            public required string m_heroName;
            public required string m_unlockName;
            public required ulong? m_voiceSetGUID;
            public required Combo.ComboInfo m_info;
            public Combo.ComboInfo m_baseInfo = null;
            public Dictionary<ulong, ulong> m_replacements = null;
            public bool m_ignoreGroups = false;
            
            public CriteriaContext m_criteriaContext;

            public SaveSetContext() {
            }
        }

        public static bool SaveVoiceSet(ExtractFlags flags, string basePath, string heroName, string unlockName, ulong? voiceSetGuid, ref Combo.ComboInfo info, Combo.ComboInfo baseCombo = null, Dictionary<ulong, ulong> replacements = null, bool ignoreGroups = false) {
            info = new Combo.ComboInfo();
            return SaveVoiceSet(new SaveSetContext {
                m_flags = flags,
                m_basePath = basePath,
                m_heroName = heroName,
                m_unlockName = unlockName,
                m_voiceSetGUID = voiceSetGuid,
                m_info = info,
                m_baseInfo = baseCombo,
                m_replacements = replacements,
                m_ignoreGroups = ignoreGroups
            });
        }

        public static bool SaveVoiceSet(SaveSetContext context) {
            if (context.m_voiceSetGUID == null) {
                return false;
            }

            var flags = context.m_flags;
            var basePath = context.m_basePath;
            var heroName = context.m_heroName;
            var unlockName = context.m_unlockName;
            var voiceSetGuid = context.m_voiceSetGUID;
            var info = context.m_info;
            var baseCombo = context.m_baseInfo;
            var replacements = context.m_replacements;
            var ignoreGroups = context.m_ignoreGroups;
            
            Combo.Find(info, voiceSetGuid.Value, replacements);

            var skinnedVoiceSet = Combo.GetReplacement(voiceSetGuid.Value, replacements);
            if (!info.m_voiceSets.ContainsKey(skinnedVoiceSet)) {
                if (Program.Client.ContainerHandler is StaticContainerHandler) {
                    // (steam)
                    Logger.Error("ExtractHeroVoice", $"Unable to load voice data {voiceSetGuid:X16}. Did you choose the correct voice locale and is the voice audio depot downloaded?");
                    return false;
                }
                Logger.Error("ExtractHeroVoice", $"Unable to load voice data {voiceSetGuid:X16}");
                return false;
            }

            // if we're processing a skin, baseCombo is the combo from the hero, this remove duplicate check removes any sounds that belong to the base hero
            // this ensures you only get sounds unique to the skin when processing a skin
            if (baseCombo != null) {
                if (!Combo.RemoveDuplicateVoiceSetEntries(baseCombo, ref info, voiceSetGuid.Value, skinnedVoiceSet))
                    return false;
            }

            foreach (var voiceSet in info.m_voiceSets) {
                if (voiceSet.Value.VoiceLineInstances == null) continue;

                foreach (var voicelineInstanceInfo in voiceSet.Value.VoiceLineInstances) {
                    foreach (var voiceLineInstance in voicelineInstanceInfo.Value) {
                        // don't add this back !!
                        // some hamster lines are non-voice audio only
                        //if (!voiceLineInstance.SoundFiles.Any()) {
                        //    continue;
                        //}

                        var stimulus = GetInstance<STUVoiceStimulus>(voiceLineInstance.VoiceStimulus);
                        if (stimulus == null) continue;

                        var groupName = GetVoiceGroup(voiceLineInstance.VoiceStimulus, stimulus.m_category) ??
                                        Path.Combine("Unknown",$"{teResourceGUID.Index(voiceLineInstance.VoiceStimulus):X}.{teResourceGUID.Type(voiceLineInstance.VoiceStimulus):X3}");

                        var stack = new List<string> { basePath };

                        CalculatePathStack(flags, heroName, unlockName, ignoreGroups ? "" : groupName, stack);

                        var path = Path.Combine(stack.Where(x => x.Length > 0).ToArray());

                        stack.Clear();
                        stack.Add(basePath);

                        string hero03FDir;
                        if (flags.VoiceGroup03FInType) {
                            hero03FDir = Path.Combine(path, "03F");
                        } else {
                            CalculatePathStack(flags, heroName, unlockName, "03F", stack);
                            hero03FDir = Path.Combine(stack.ToArray());
                        }

                        // 99% of voiceline instances only have a single sound file however there are cases where some NPCs have multiple
                        // the Junkenstein Narrator is an example, the lines are the same however they are spoken differently.
                        foreach (var soundFile in voiceLineInstance.SoundFiles) {
                            var soundFileGuid = teResourceGUID.AsString(soundFile);
                            string filename = null;

                            if (!flags.VoiceGroupByHero && !ignoreGroups) {
                                filename = $"{heroName}-{soundFileGuid}";
                            }

                            if (SoundIdCache.Contains(soundFile)) {
                                Logger.Debug("Tool", "Duplicate sound detected, ignoring.");
                                continue;
                            }

                            SoundIdCache.Add(soundFile);
                            //SaveLogic.Combo.SaveSoundFile(flags, path, saveContext, soundFile, true, filename);
                            SaveLogic.Combo.SaveVoiceLineInstance(flags, path, voiceLineInstance, filename, soundFile);
                        }

                        if (voiceLineInstance.m_criteria != null && context.m_criteriaContext != null) {
                            var stringWriter = new StringWriter();
                            var indentedWriter = new IndentedTextWriter(stringWriter);
                            indentedWriter.WriteLine(IO.GetSubtitleString(voiceLineInstance.Subtitle));
                            indentedWriter.Indent++;
                            BuildCriteriaDescription(indentedWriter, voiceLineInstance.m_criteria, context.m_criteriaContext);
                            Console.Out.Write(stringWriter.ToString());
                            indentedWriter.Indent--;
                        }

                        // Saves Wrecking Balls squeak sounds, no other heroes have sounds like this it seems
                        var stuSound = GetInstance<STUSound>(voiceLineInstance.ExternalSound);
                        if (stuSound?.m_C32C2195?.m_soundWEMFiles != null) {
                            foreach (var mSoundWemFile in stuSound?.m_C32C2195?.m_soundWEMFiles) {
                                if (BadSoundFiles.Contains(mSoundWemFile)) {
                                    continue;
                                }

                                SaveLogic.Combo.SaveSoundFile(flags, hero03FDir, mSoundWemFile);
                            }
                        }
                    }
                }
            }

            return true;
        }

        private static void BuildCriteriaDescription(IndentedTextWriter writer, STUCriteriaContainer container, CriteriaContext context) {
            if (container is not STU_32A19631 embedCriteria) {
                // I don't think the asset ref version is used
                // probably deleted by build pipeline
                writer.WriteLine("Error: non-embedded criteria");
                return;
            }

            BuildCriteriaDescription(writer, embedCriteria.m_criteria, context);
        }

        private static void BuildCriteriaDescription(IndentedTextWriter writer, STUCriteria criteria, CriteriaContext context) {
            switch (criteria) {
                case null:
                    writer.WriteLine("Error: null criteria");
                    break;
                case STUCriteria_Statescript statescript: {
                    using var _ = new ModifierScope(writer, statescript.m_57D96E27, "NOT");
                    
                    writer.Write($"Scripted Event: {teResourceGUID.Index(statescript.m_identifier):X}");
                    break;
                }
                case STU_D815520F heroInteraction: {
                    using var _ = new ModifierScope(writer, heroInteraction.m_57D96E27, "NOT");
                    
                    // kill lines
                    writer.Write($"Hero Interaction: {context.GetHeroName(heroInteraction.m_8C8C5285)}");
                    break;
                }
                case STUCriteria_IsHero isHero:
                    // also kill lines...
                    // not clear what the difference is
                    writer.Write($"Is Hero: {context.GetHeroName(isHero.m_hero)}");
                    break;
                
                case STU_3EAADDE8 teamInteraction: {
                    // e.g "Look at us! The full might of Overwatch, reassembled and ready to rumble!"
                    using var _ = new ModifierScope(writer, teamInteraction.m_990CFF1C, "NOT");
                    
                    if (teamInteraction.m_hero != 0) {
                        writer.Write($"Hero On Team: {context.GetHeroName(teamInteraction.m_hero)}");
                    } else {
                        writer.Write($"Tag On Teammate: {context.GetTagName(teamInteraction.m_7D7C86A1)}");
                    }
                    break;
                }
                case STUCriteria_Team team:
                    writer.WriteLine($"On Team Number: {team.m_team}. UnkBool: {team.m_EB5492C4 != 0}");
                    break;
                
                case STU_A95E4B99 gender:
                    // specializing lines for different pronouns
                    writer.WriteLine($"Required Gender: {gender.m_gender}");
                    break;
                case STU_C9F4617F gender2:
                    // same thing...
                    writer.WriteLine($"Required Gender: {gender2.m_gender}");
                    break;
                
                case STU_C37857A5 celebration:
                    writer.WriteLine($"Active Celebration: {context.GetCelebrationName(celebration.m_celebrationType)}");
                    break;
                case STUCriteria_OnMap onMap: 
                    writer.WriteLine($"On Map: {context.GetMapName(onMap.m_map)}. Allow Event Variants: {(onMap.m_exactMap != 0 ? "false" : "true")}");
                    break;
                case STU_4A7A3740 onGameMode: {
                    // NOT used by volleyball of all things, to override ult lines
                    using var _ = new ModifierScope(writer, onGameMode.m_E9A758B4, "NOT");
                    
                    writer.Write($"On Game Mode: {context.GetGameModeName(onGameMode.m_gameMode)}");
                    break;
                }
                case STU_0F78DDB0 onMission: {
                    using var _ = new ModifierScope(writer, onMission.m_89B967D3, "NOT");
                    
                    writer.Write($"On Mission: {context.GetMissionName(onMission.m_216EA6DA)}");
                    break;
                }
                case STU_20ABB515 onObjective:
                    // usually used with a mission criteria, even though objectives are mission specific
                    writer.WriteLine($"On Mission Objective: {context.GetObjectiveName(onObjective.m_4992CB75)}");
                    break;
                case STU_A9B89EC9 pve3:
                    // used for wotb
                    // Bet you I find the next key!
                    //    STU_A9B89EC9: 000000008253.01C
                    writer.WriteLine($"STU_A9B89EC9: {pve3.m_98D3EC50?.m_id}");
                    break;
                case STU_31297254 unk31297254:
                    // todo: talent (or perk ig now.. surely not)
                    // STU_31297254: 0000000002E8.134. bool: False
                    writer.WriteLine($"STU_31297254: {unk31297254.m_91A9D4CC}. bool: {unk31297254.m_8F034FB5 != 0}");
                    break;
                case STU_9665B416 unk9665B416:
                    writer.WriteLine($"STU_9665B416: {unk9665B416.m_EF135378?.m_id}");
                    break;
                case STU_E6EBD07B unkE6EBD07B:
                    writer.WriteLine($"STU_E6EBD07B: {unkE6EBD07B.m_E755B82A?.m_id}");
                    break;
                
                case STU_7C69EA0F nestedContainer: {
                    writer.WriteLine($"Nested - {nestedContainer.m_amount}/{nestedContainer.m_criteria.Length} Required:");
                    writer.Indent++;
                    foreach (var nested in nestedContainer.m_criteria) {
                        BuildCriteriaDescription(writer, nested, context);
                    }
                    writer.Indent--;
                    break;
                }
                default: {
                    writer.WriteLine($"Unknown: {criteria.GetType().Name}");
                    
                    // debug: exit on unknown found (you should turn off saving also)
                    // Console.Out.WriteLine(writer.InnerWriter.ToString());
                    // Environment.Exit(0);
                    break;
                }
            }
        }

        private ref struct ModifierScope : IDisposable {
            private readonly IndentedTextWriter m_writer;
            private readonly bool m_active;
            
            public ModifierScope(IndentedTextWriter writer, byte condition, string op) {
                // this would not work over an entire container for example... but doesn't seem needed
                // also the only operator seems to be NOT
                
                m_writer = writer;
                m_active = condition != 0;
                if (m_active) {
                    writer.Write($"{op} (");
                }
            }
            
            public void Dispose() {
                if (m_active) {
                    m_writer.Write(")");
                }
                m_writer.WriteLine();
            }
        }

        private static void CalculatePathStack(ExtractFlags flags, string heroName, string unlockName, string groupName, List<string> stack) {
            if (flags.VoiceGroupByLocale) {
                stack.Add(Program.Client.CreateArgs.SpeechLanguage ?? "enUS");
            }

            if (flags.VoiceGroupByHero) {
                stack.Add(heroName);

                if (flags.VoiceGroupBySkin) {
                    stack.Add(unlockName);
                }

                if (flags.VoiceGroupByType) {
                    stack.Add(groupName);
                }
            } else if (flags.VoiceGroupByType) {
                stack.Add(groupName);
            }
        }

        public static string GetVoiceGroup(ulong stimulusGuid, ulong categoryGuid) {
            return GetNullableGUIDName(stimulusGuid) ?? GetNullableGUIDName(categoryGuid);
        }

        // these are weird UI sounds that seem to be on every STUSound
        public static readonly HashSet<ulong> BadSoundFiles = new HashSet<ulong>() {
            0x7C0000000147482, // 000000147482.03F
            0x7C00000000958E8, // 0000000958E8.03F
            0x7C00000000958E6, // 0000000958E6.03F
            0x7C00000001BD800, // 0000001BD800.03F
            0x07C00000001BD7FF, // 0000001BD7FF.03F
        };
    }
}
