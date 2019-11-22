using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.JSON;
using DataTool.ToolLogic.Extract;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Dbg {
    [Tool("extract-hero-voice-better", Description = "Extracts hero voicelines but groups them a bit better.", CustomFlags = typeof(ExtractFlags))]
    class ExtractHeroVoiceBetter : JSONTool, ITool {
        class VoiceGroup {
            public string Name;
            public string[] StimulusSetIds = new string[0];
            public string[] StimulusUnknownIds = new string[0];
            public string[] StimulusCategoryIds = new string[0];
        }
        
        private const string Container = "BetterHeroVoice";

        private List<VoiceGroup> VoiceGroups = new List<VoiceGroup>() {
            new VoiceGroup { Name = "Ultimate",        StimulusUnknownIds = new []{ "000000000079.08A" } },
            new VoiceGroup { Name = "Voicelines",      StimulusCategoryIds = new []{ "000000000031.079" } },
            new VoiceGroup { Name = "Hello",           StimulusSetIds = new []{ "0000000001CF.078" } },
            new VoiceGroup { Name = "Bye",             StimulusSetIds = new []{ "0000000002D4.078" } },
            new VoiceGroup { Name = "No",              StimulusSetIds = new []{ "0000000001D2.078" } },
            new VoiceGroup { Name = "Yes",             StimulusSetIds = new []{ "0000000001D1.078" } },
            new VoiceGroup { Name = "Healing",         StimulusSetIds = new []{ "000000000045.078" } },
            new VoiceGroup { Name = "GroupUp",         StimulusSetIds = new []{ "0000000002C6.078" } },
            new VoiceGroup { Name = "Incoming",        StimulusSetIds = new []{ "000000000112.078" } },
            new VoiceGroup { Name = "Understand",      StimulusSetIds = new []{ "0000000002F2.078" } },
            new VoiceGroup { Name = "Thanks",          StimulusSetIds = new []{ "0000000001D0.078" } },
            new VoiceGroup { Name = "UltimateReady",   StimulusSetIds = new []{ "0000000002E7.078" } },
            new VoiceGroup { Name = "Respawn",         StimulusSetIds = new []{ "00000000002C.078" } },
            new VoiceGroup { Name = "NanoBoosted",     StimulusSetIds = new []{ "0000000005E2.078" } },
            new VoiceGroup { Name = "DamageBoosted",   StimulusSetIds = new []{ "0000000000CC.078" } },
            new VoiceGroup { Name = "Unwell",          StimulusSetIds = new []{ "0000000005ED.078" } },
            new VoiceGroup { Name = "HeroChange",      StimulusSetIds = new []{ "00000000014B.078" } },
            new VoiceGroup { Name = "HeroSelect",      StimulusSetIds = new []{ "00000000002B.078" } },
            new VoiceGroup { Name = "Discorded",       StimulusSetIds = new []{ "0000000002BF.078" } },
            new VoiceGroup { Name = "VotedEpic",       StimulusSetIds = new []{ "00000000047D.078" } },
            new VoiceGroup { Name = "VotedLegendary",  StimulusSetIds = new []{ "000000000496.078" } },
            new VoiceGroup { Name = "OnFire",          StimulusSetIds = new []{ "000000000583.078" } },
            new VoiceGroup { Name = "HealthPack",      StimulusSetIds = new []{ "00000000002A.078" } },
            new VoiceGroup { Name = "EnemyResurrect",  StimulusSetIds = new []{ "00000000011B.078" } },
            new VoiceGroup { Name = "Resurrected",     StimulusSetIds = new []{ "00000000005A.078" } },
            new VoiceGroup { Name = "Elimination",     StimulusSetIds = new []{ "000000000008.078" } },
            new VoiceGroup { Name = "KillStreak",      StimulusSetIds = new []{ "000000000039.078" } },
            new VoiceGroup { Name = "TeamElimination", StimulusSetIds = new []{ "00000000001B.078" } },
            new VoiceGroup { Name = "Revenge",         StimulusSetIds = new []{ "000000000036.078" } },
            new VoiceGroup { Name = "Fallback",        StimulusSetIds = new []{ "000000000111.078" } },
            new VoiceGroup { Name = "EnemyTeleporter", StimulusSetIds = new []{ "0000000002F0.078" } },
            new VoiceGroup { Name = "GoingIn",         StimulusSetIds = new []{ "0000000002E8.078" } },
            new VoiceGroup { Name = "ImWithYou",       StimulusSetIds = new []{ "00000000010F.078" } },
            new VoiceGroup { Name = "OnMyWay",         StimulusSetIds = new []{ "0000000002E9.078" } },
            new VoiceGroup { Name = "MovePayload",     StimulusSetIds = new []{ "0000000002EC.078" } },
            new VoiceGroup { Name = "ImReady",         StimulusSetIds = new []{ "0000000002F1.078" } },
            new VoiceGroup { Name = "GetReady",        StimulusSetIds = new []{ "000000000573.078" } },
            new VoiceGroup { Name = "GetInThere",      StimulusSetIds = new []{ "0000000002C5.078" } },
            new VoiceGroup { Name = "RescueTeammate",  StimulusSetIds = new []{ "0000000000CA.078" } },
            new VoiceGroup { Name = "TeamKill",        StimulusSetIds = new []{ "000000000584.078", "000000000584.078" } }, // why is there 2
            new VoiceGroup { Name = "UnderAttack",     StimulusSetIds = new []{ "0000000009C6.078" } }, // supports only
        };

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

                foreach (var voiceSet in voiceSetsCombo.VoiceSets) {
                    if (voiceSet.Value.VoiceLineInstances == null) continue;

                    foreach (var voicelineInstanceInfo in voiceSet.Value.VoiceLineInstances) {
                        foreach (var voiceLineInstance in voicelineInstanceInfo.Value) {
                            var soundFilesCombo = new Combo.ComboInfo();;
                            var stimulus = GetInstance<STUVoiceStimulus>(voiceLineInstance.VoiceStimulus);

                            if (stimulus == null) continue;

                            var stimulusId = teResourceGUID.AsString(voiceLineInstance.VoiceStimulus);
                            var stimulusCategoryId = teResourceGUID.AsString(stimulus.m_category);
                            var stimulusUnknownId = teResourceGUID.AsString(stimulus.m_87DCD58E);
                            
                            // Some voices can be classified in multiple groups but we can only have one, we use use the Stimulus name first, then the unknown and finally the category
                            var stimMatches = VoiceGroups.FirstOrDefault(x => x.StimulusSetIds.Contains(stimulusId));
                            var stimCatMatches = VoiceGroups.FirstOrDefault(x => x.StimulusCategoryIds.Contains(stimulusCategoryId));
                            var stimUnkMatches = VoiceGroups.FirstOrDefault(x => x.StimulusUnknownIds.Contains(stimulusUnknownId));
                            
                            if (stimMatches == null && stimCatMatches == null && stimUnkMatches == null) continue;

                            var groupName = stimMatches?.Name ?? stimUnkMatches?.Name ?? stimCatMatches?.Name;
                            
                            foreach (var soundFile in voiceLineInstance.SoundFiles) {
                                Combo.Find(soundFilesCombo, soundFile);
                            }
                            
                            SaveLogic.Combo.SaveAllVoiceSoundFiles(toolFlags, Path.Combine(basePath, Container, groupName, heroNameActual), soundFilesCombo);                            
                        }
                    }
                }
            }
        }
    }
}
