using System.Collections.Generic;
using System.IO;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using OWLib;
using OWLib.Types.Chunk;
using TankLib.STU.Types;

namespace DataTool.SaveLogic.Unlock {
    public static class VoiceLine {
        public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock, VoiceSet voiceSet) {
            if (voiceSet == null) return;
            
            if (!(unlock.STU is STUUnlock_VoiceLine vl)) return;

            HashSet<ulong> voiceLines = new HashSet<ulong>();
            using (Stream vlStream = IO.OpenFile(vl.m_F57B051E)) {
                using (Chunked vlChunk = new Chunked(vlStream)) {
                    foreach (SVCE svce in vlChunk.GetAllOfTypeFlat<SVCE>()) {
                        if (svce == null) continue;

                        if (voiceSet.Stimuli.ContainsKey(svce.Data.VoiceStimulus)) {
                            foreach (var voiceLine in voiceSet.Stimuli[svce.Data.VoiceStimulus]) {
                                voiceLines.Add(voiceLine);
                            }
                        }
                    }
                }
            }
            
            SaveVoiceLines(flags, voiceLines, voiceSet, directory);
        }
        
        public static void SaveVoiceLines(ICLIFlags flags, HashSet<ulong> lines, VoiceSet voiceSet, string directory) {
            FindLogic.Combo.ComboInfo fakeComboInfo = new FindLogic.Combo.ComboInfo();

            foreach (ulong line in lines) {
                VoiceLineInstance voiceLineInstance = voiceSet.VoiceLines[line];

                SaveVoiceLine(flags, voiceLineInstance, directory, fakeComboInfo);
            }
        }

        public static void SaveVoiceLine(ICLIFlags flags, VoiceLineInstance voiceLineInstance, string directory, FindLogic.Combo.ComboInfo combo) {
            foreach (ulong soundFile in voiceLineInstance.VoiceSounds) {
                FindLogic.Combo.SoundFileInfo fakeSoundFileInfo = new FindLogic.Combo.SoundFileInfo(soundFile);
                combo.VoiceSoundFiles[soundFile] = fakeSoundFileInfo;
                
                Combo.SaveSoundFile(flags, directory, combo, soundFile, true);
            }
        }
    }
}