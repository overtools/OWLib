using System;
using System.IO;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract {
    
    [Tool("extract-hero-voice-better", Description = "Extracts hero voicelines but groups them a bit better.", CustomFlags = typeof(ExtractFlags))]
    class ExtractHeroVoiceBetter : JSONTool, ITool {
        private const string Container = "BetterHeroVoice";

        public void Parse(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            foreach (var key in Program.TrackedFiles[0x75]) {
                var hero = GetInstance<STUHero>(key);
                var progression = new ProgressionUnlocks(hero);
                if (progression.LootBoxesUnlocks == null) continue; // no NPCs thanks
                
                string heroNameActual = GetValidFilename((GetString(hero.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(key)}").TrimEnd(' '));
                var voiceSetComponent = GetInstance<STUVoiceSetComponent>(hero.m_gameplayEntity);
                if (voiceSetComponent?.m_voiceDefinition == null) continue;

                var voiceSetsCombo = new Combo.ComboInfo();
                Combo.Find(voiceSetsCombo, voiceSetComponent.m_voiceDefinition);

                foreach (var voiceSet in voiceSetsCombo.m_voiceSets) {
                    if (voiceSet.Value.VoiceLineInstances == null) continue;

                    foreach (var voicelineInstanceInfo in voiceSet.Value.VoiceLineInstances) {
                        foreach (var voiceLineInstance in voicelineInstanceInfo.Value) {
                            var stimulus = GetInstance<STUVoiceStimulus>(voiceLineInstance.VoiceStimulus);
                            if (stimulus == null) continue;
                            
                            var groupName = GetVoiceGroup(voiceLineInstance.VoiceStimulus, stimulus.m_category, stimulus.m_87DCD58E) ?? "Unknown";
                            
                            var soundFilesCombo = new Combo.ComboInfo();
                            var soundFilesContext = new SaveLogic.Combo.SaveContext(soundFilesCombo);
                            
                            foreach (var soundFile in voiceLineInstance.SoundFiles) {
                                Combo.Find(soundFilesCombo, soundFile);
                            }

                            var path = flags.GroupByHero && flags.GroupByType
                                ? Path.Combine(basePath, Container, heroNameActual, groupName)
                                : Path.Combine(basePath, Container, flags.GroupByHero ? Path.Combine(groupName, heroNameActual) : groupName);

                            foreach (var soundInfo in soundFilesCombo.m_voiceSoundFiles.Values) {
                                var filename = soundInfo.GetName();
                                if (!flags.GroupByHero) {
                                    filename = $"{heroNameActual}-{soundInfo.GetName()}";
                                }
                                
                                SaveLogic.Combo.SaveSoundFile(flags, path, soundFilesContext, soundInfo.m_GUID, true, filename);
                            }
                            soundFilesContext.Wait();
                        }
                    }
                }
            }
        }

        public static string GetVoiceGroup(ulong stimulusGuid, ulong categoryGuid, ulong unkGuid)
        {
            return GetNullableGUIDName(stimulusGuid) ?? GetNullableGUIDName(categoryGuid) ?? GetNullableGUIDName(unkGuid);
        }
    }
}
