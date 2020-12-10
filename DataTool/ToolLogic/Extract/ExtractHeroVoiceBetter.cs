using System;
using System.Collections.Generic;
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
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            
            // Do normal heroes first then NPCs, this is because NPCs have a lot of duplicate sounds and normal heroes (should) have none
            // so any duplicate sounds would only come up while processing NPCs which can be ignored as they (probably) belong to heroes
            var heroes = Program.TrackedFiles[0x75].Select(x => new Hero(x)).OrderBy(x => !x.IsHero).ThenBy(x => x.GUID.GUID).ToArray();

            foreach (var hero in heroes) {
                var heroStu = GetInstance<STUHero>(hero.GUID);

                string heroNameActual = GetValidFilename((GetString(heroStu.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(hero.GUID)}").TrimEnd(' '));
                Logger.Log($"Processing {heroNameActual}");
                var voiceSetComponent = GetInstance<STUVoiceSetComponent>(heroStu.m_gameplayEntity);
                
                STUVoiceSetComponent baseComponent = default;
                Combo.ComboInfo baseInfo = default;

                if (SaveSet(flags, basePath, heroStu.m_gameplayEntity, heroNameActual, ref baseComponent, ref baseInfo)) {
                    
                    var skins = new ProgressionUnlocks(heroStu).GetUnlocksOfType(UnlockType.Skin);
                    foreach (var unlock in skins) {
                        TACTLib.Logger.Debug("Tool", $"Processing skin {unlock.Name}");
                        if (!(unlock.STU is STUUnlock_SkinTheme unlockSkinTheme)) return;
                        if (unlockSkinTheme.m_0B1BA7C1 != 0)
                            continue;
                        
                        var skinTheme = GetInstance<STUSkinTheme>(unlockSkinTheme.m_skinTheme);
                        if (skinTheme == null)
                            continue;

                        STUVoiceSetComponent component = default;
                        Combo.ComboInfo info = default;

                        SaveSet(flags, basePath, heroStu.m_gameplayEntity, heroNameActual, ref component, ref info, baseComponent, baseInfo, SkinTheme.GetReplacements(skinTheme));
                    }
                }
            }
        }

        private static bool SaveSet(ExtractFlags flags, string basePath, ulong entityMain, string heroNameActual, ref STUVoiceSetComponent voiceSetComponent, ref Combo.ComboInfo info, STUVoiceSetComponent baseComponent = null, Combo.ComboInfo baseCombo = null, Dictionary<ulong, ulong> replacements = null) {
            voiceSetComponent = GetInstance<STUVoiceSetComponent>(Combo.GetReplacement(entityMain, replacements));

            if (voiceSetComponent?.m_voiceDefinition == null) {
                return false;
            }

            info = new Combo.ComboInfo();
            Combo.Find(info, Combo.GetReplacement(voiceSetComponent.m_voiceDefinition, replacements), replacements);
            if (baseComponent != null && baseCombo != null) {
                if (!Combo.RemoveDuplicateVoiceSetEntries(baseCombo, ref info, baseComponent.m_voiceDefinition, Combo.GetReplacement(voiceSetComponent.m_voiceDefinition, replacements)))
                    return false;
            }
            
            foreach (var voiceSet in info.m_voiceSets) {
                if (voiceSet.Value.VoiceLineInstances == null) continue;

                foreach (var voicelineInstanceInfo in voiceSet.Value.VoiceLineInstances) {
                    foreach (var voiceLineInstance in voicelineInstanceInfo.Value) {
                        var stimulus = GetInstance<STUVoiceStimulus>(voiceLineInstance.VoiceStimulus);
                        if (stimulus == null) continue;

                        var groupName = GetVoiceGroup(voiceLineInstance.VoiceStimulus, stimulus.m_category, stimulus.m_87DCD58E);
                        if (groupName == null)
                            groupName = $"Unknown\\{teResourceGUID.Index(voiceLineInstance.VoiceStimulus):X}.{teResourceGUID.Type(voiceLineInstance.VoiceStimulus):X3}";

                        var soundFilesCombo = new Combo.ComboInfo();
                        var soundFilesContext = new SaveLogic.Combo.SaveContext(soundFilesCombo);

                        foreach (var soundFile in voiceLineInstance.SoundFiles) {
                            Combo.Find(soundFilesCombo, soundFile);
                        }

                        var path = flags.VoiceGroupByHero && flags.VoiceGroupByType
                            ? Path.Combine(basePath, Container, heroNameActual, groupName)
                            : Path.Combine(basePath, Container, flags.VoiceGroupByHero ? Path.Combine(groupName, heroNameActual) : groupName);

                        foreach (var soundInfo in soundFilesCombo.m_voiceSoundFiles.Values) {
                            var filename = soundInfo.GetName();
                            if (!flags.VoiceGroupByHero) {
                                filename = $"{heroNameActual}-{soundInfo.GetName()}";
                            }

                            if (SoundIdCache.Contains(soundInfo.m_GUID)) {
                                TACTLib.Logger.Debug("Tool", "Duplicate sound detected, ignoring.");
                                continue;
                            }

                            SoundIdCache.Add(soundInfo.m_GUID);
                            SaveLogic.Combo.SaveSoundFile(flags, path, soundFilesContext, soundInfo.m_GUID, true, filename);
                        }

                        soundFilesContext.Wait();
                    }
                }
            }

            return true;
        }

        public static string GetVoiceGroup(ulong stimulusGuid, ulong categoryGuid, ulong unkGuid)
        {
            return GetNullableGUIDName(stimulusGuid) ?? GetNullableGUIDName(categoryGuid) ?? GetNullableGUIDName(unkGuid);
        }
    }
}
