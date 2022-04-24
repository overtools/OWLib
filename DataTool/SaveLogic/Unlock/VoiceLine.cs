using System.Collections.Generic;
using System.IO;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;
using TankLib.Chunks;
using TankLib.STU.Types;

namespace DataTool.SaveLogic.Unlock {
    public static class VoiceLine {
        public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock, VoiceSet voiceSet) {
            if (voiceSet == null) return;

            if (!(unlock.STU is STUUnlock_VoiceLine vl)) return;

            HashSet<ulong> voiceLines = new HashSet<ulong>();
            using (Stream vlStream = IO.OpenFile(vl.m_F57B051E)) {
                teChunkedData chunkedData = new teChunkedData(vlStream);

                foreach (teEffectComponentVoiceStimulus voiceStimulus in chunkedData.GetChunks<teEffectComponentVoiceStimulus>()) {
                    if (voiceSet.Stimuli.TryGetValue(voiceStimulus.Header.VoiceStimulus, out var stimuliLines)) {
                        foreach (var voiceLine in stimuliLines) {
                            voiceLines.Add(voiceLine);
                        }
                    }
                }
            }

            SaveVoiceLines(flags, voiceLines, voiceSet, directory);
        }

        public static void SaveVoiceLines(ICLIFlags flags, HashSet<ulong> lines, VoiceSet voiceSet, string directory) {
            FindLogic.Combo.ComboInfo fakeComboInfo = new FindLogic.Combo.ComboInfo();
            var saveContext = new Combo.SaveContext(fakeComboInfo);

            foreach (ulong line in lines) {
                VoiceLineInstance voiceLineInstance = voiceSet.VoiceLines[line];

                SaveVoiceLine(flags, voiceLineInstance, directory, saveContext);
            }
        }

        public static void SaveVoiceLine(ICLIFlags flags, VoiceLineInstance voiceLineInstance, string directory, Combo.SaveContext context) {
            if (voiceLineInstance.VoiceSounds == null) return;
            foreach (ulong soundFile in voiceLineInstance.VoiceSounds) {
                FindLogic.Combo.SoundFileAsset fakeSoundFileInfo = new FindLogic.Combo.SoundFileAsset(soundFile);
                context.m_info.m_voiceSoundFiles[soundFile] = fakeSoundFileInfo;

                Combo.SaveSoundFile(flags, directory, context, soundFile, true);
            }
        }
    }
}
