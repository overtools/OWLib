using System;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using STULib.Types;
using static DataTool.Program;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-voice-master", Description = "Extract voice master (debug)", TrackTypes = new ushort[] {0x5F}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugVoiceMaster : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ExtractVoiceMasters(toolFlags);
        }

        public void ExtractVoiceMasters(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            const string container = "DebugVoiceMaster";
            
            foreach (ulong key in TrackedFiles[0x5F]) {
                STUVoiceMaster voiceMaster = GetInstance<STUVoiceMaster>(key);

                string voiceMaterDir = Path.Combine(basePath, container, GetFileName(key));

                foreach (STUVoiceLineInstance voiceLineInstance in voiceMaster.VoiceLineInstances) {
                    if (voiceLineInstance?.SoundDataContainer == null) continue;
                    
                    Combo.ComboInfo info = new Combo.ComboInfo();

                    Combo.Find(info, voiceLineInstance.SoundDataContainer.SoundbankMasterResource);

                    foreach (ulong soundInfoNew in info.Sounds.Keys) {
                        SaveLogic.Combo.SaveSound(flags, voiceMaterDir, info, soundInfoNew);
                    }
                }
            }
        }
    }
}