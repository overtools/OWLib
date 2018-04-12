using System;
using System.IO;
using DataTool.Flag;
using OWLib;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-movies", Description = "Extract movies (debug)", TrackTypes = new ushort[] {0xB6}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugMovies : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ExtractVoiceSets(toolFlags);
        }

        public void ExtractVoiceSets(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            const string container = "DebugMovies";
            
            foreach (ulong key in Program.TrackedFiles[0xB6]) {
                using (Stream videoStream = OpenFile(key)) {
                    if (videoStream != null) {
                        videoStream.Position = 128;  // wrapped in "MOVI" for some reason
                        WriteFile(videoStream, Path.Combine(basePath, container, $"{GUID.LongKey(key):X12}.bk2"));
                    }
                }
            }
        }
    }
}