using System;
using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using STULib.Types.Generic;
using static DataTool.Program;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-sound", Description = "Extract sounds (debug)", TrackTypes = new ushort[] {0x5F}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugSound : ITool {
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
            
            
            foreach (ulong key in TrackedFiles[0x5F]) {
                Dictionary<ulong, List<SoundInfo>> sounds = new Dictionary<ulong, List<SoundInfo>>();
                Sound.FindSounds(sounds, new Common.STUGUID(key));
                // SaveLogic.Sound.Save(flags, Path.Combine(basePath, GetFileName(key)) + Path.DirectorySeparatorChar, sounds, false);
            }
        }
    }
}