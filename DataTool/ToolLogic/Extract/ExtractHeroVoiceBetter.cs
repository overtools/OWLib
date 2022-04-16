using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;
using SkinTheme = DataTool.SaveLogic.Unlock.SkinTheme;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-hero-voice-better", Description = "Extracts hero voicelines but groups them a bit better.", CustomFlags = typeof(ExtractFlags))]
    class ExtractHeroVoiceBetter : JSONTool, ITool {
        private const string Container = "BetterHeroVoice";
        private static readonly HashSet<ulong> SoundIdCache = new HashSet<ulong>();

        public void Parse(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = Path.Combine(flags.OutputPath, Container);
            } else {
                throw new Exception("no output path");
            }

            // Do normal heroes first then NPCs, this is because NPCs have a lot of duplicate sounds and normal heroes (should) have none
            // so any duplicate sounds would only come up while processing NPCs which can be ignored as they (probably) belong to heroes
            var heroes = Program.TrackedFiles[0x75]
                .Select(x => new Hero(x))
                .OrderBy(x => !x.IsHero)
                .ThenBy(x => x.GUID.GUID)
                .ToArray();

            foreach (var hero in heroes) {
                var heroStu = GetInstance<STUHero>(hero.GUID);

                string heroName = GetValidFilename((GetString(heroStu.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(hero.GUID)}").TrimEnd(' '));
                Logger.Log($"Processing {heroName}");

                Combo.ComboInfo baseInfo = default;
                var heroVoiceSetGuid = GetInstance<STUVoiceSetComponent>(heroStu.m_gameplayEntity)?.m_voiceDefinition;

                if (SaveVoiceSet(flags, basePath, heroName, heroVoiceSetGuid, ref baseInfo)) {
                    var skins = new ProgressionUnlocks(heroStu).GetUnlocksOfType(UnlockType.Skin);

                    foreach (var unlock in skins) {
                        var unlockSkinTheme = unlock.STU as STUUnlock_SkinTheme;
                        if (unlockSkinTheme?.m_0B1BA7C1 != 0) {
                            continue; // no idea what this is
                        }

                        TACTLib.Logger.Debug("Tool", $"Processing skin {unlock.Name}");
                        Combo.ComboInfo info = default;
                        var skinTheme = GetInstance<STUSkinTheme>(unlockSkinTheme.m_skinTheme);
                        if (skinTheme == null) {
                            continue;
                        }

                        SaveVoiceSet(flags, basePath, heroName, heroVoiceSetGuid, ref info, baseInfo, SkinTheme.GetReplacements(skinTheme));
                    }
                }
            }
        }

        public static bool SaveVoiceSet(ExtractFlags flags, string basePath, string heroName, ulong? voiceSetGuid, ref Combo.ComboInfo info, Combo.ComboInfo baseCombo = null, Dictionary<ulong, ulong> replacements = null, bool ignoreGroups = false) {
            if (voiceSetGuid == null) {
                return false;
            }

            info = new Combo.ComboInfo();
            var saveContext = new SaveLogic.Combo.SaveContext(info);
            Combo.Find(info, voiceSetGuid.Value, replacements);

            // if we're processing a skin, baseCombo is the combo from the hero, this remove duplicate check removes any sounds that belong to the base hero
            // this ensures you only get sounds unique to the skin when processing a skin
            if (baseCombo != null) {
                if (!Combo.RemoveDuplicateVoiceSetEntries(baseCombo, ref info, voiceSetGuid.Value, Combo.GetReplacement(voiceSetGuid.Value, replacements)))
                    return false;
            }

            foreach (var voiceSet in info.m_voiceSets) {
                if (voiceSet.Value.VoiceLineInstances == null) continue;

                foreach (var voicelineInstanceInfo in voiceSet.Value.VoiceLineInstances) {
                    foreach (var voiceLineInstance in voicelineInstanceInfo.Value) {
                        if (!voiceLineInstance.SoundFiles.Any()) {
                            continue;
                        }

                        var stimulus = GetInstance<STUVoiceStimulus>(voiceLineInstance.VoiceStimulus);
                        if (stimulus == null) continue;

                        var groupName = GetVoiceGroup(voiceLineInstance.VoiceStimulus, stimulus.m_category, stimulus.m_87DCD58E) ??
                                        Path.Combine("Unknown",$"{teResourceGUID.Index(voiceLineInstance.VoiceStimulus):X}.{teResourceGUID.Type(voiceLineInstance.VoiceStimulus):X3}");

                        var path = flags.VoiceGroupByHero && flags.VoiceGroupByType
                                       ? Path.Combine(basePath, heroName, groupName)
                                       : Path.Combine(basePath, flags.VoiceGroupByHero ? Path.Combine(groupName, heroName) : groupName);

                        var hero03FDir = flags.VoiceGroupByHero ? Path.Combine(basePath, heroName, "03F") : Path.Combine(basePath, "03F", heroName);

                        if (ignoreGroups) {
                            path = Path.Combine(basePath, heroName);
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
                                TACTLib.Logger.Debug("Tool", "Duplicate sound detected, ignoring.");
                                continue;
                            }

                            SoundIdCache.Add(soundFile);
                            //SaveLogic.Combo.SaveSoundFile(flags, path, saveContext, soundFile, true, filename);
                            SaveLogic.Combo.SaveVoiceLineInstance(flags, path, voiceLineInstance, filename, soundFile);
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

        public static string GetVoiceGroup(ulong stimulusGuid, ulong categoryGuid, ulong unkGuid) {
            return GetNullableGUIDName(stimulusGuid) ?? GetNullableGUIDName(categoryGuid) ?? GetNullableGUIDName(unkGuid);
        }

        // these are weird UI sounds that seem to be on every STUSound
        public static readonly HashSet<ulong> BadSoundFiles = new HashSet<ulong>() {
            0x7C0000000147482, // 000000147482.03F
            0x7C00000000958E8, // 0000000958E8.03F
            0x7C00000000958E6, // 0000000958E6.03F
        };
    }
}
