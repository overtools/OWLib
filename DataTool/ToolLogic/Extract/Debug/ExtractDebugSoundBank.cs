using System;
using System.Collections.Generic;
using System.IO;
using DataTool.ConvertLogic;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using STULib.Types.Generic;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-bnk", Description = "Extract WWise Banks (debug)", TrackTypes = new ushort[] {0x2C}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugSoundBank : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            SaveSounds(toolFlags);
        }

        public void SaveSounds(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            
            
            foreach (ulong key in TrackedFiles[0x2C]) {
                if (GUID.Index(key) != 0x3BAF) continue;  // used in Reaper's Eternal Rest intro. (eerie background + door break)
                STUSound sound = GetInstance<STUSound>(key);
                Dictionary<uint, Common.STUGUID> soundIDs = new Dictionary<uint, Common.STUGUID>();
                if (sound.Inner == null) continue;
                STUSoundbankDataVersion inner = sound.Inner;
                for (int i = 0; i < inner.IDs.Length; i++) {
                    soundIDs[inner.IDs[i]] = inner.Sounds[i];
                }

                using (Stream bnkStream = OpenFile(inner.Soundbank)) {
                    Sound.WwiseBank bank = new Sound.WwiseBank(bnkStream);
                }
            }
        }
    }
}