using System;
using System.Collections.Generic;
using System.IO;
using DataTool.Flag;
using static DataTool.Program;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-ram-test", Description = "Ram test (debug)", TrackTypes = new ushort[] {0x4}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugRAMTest : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            RAMTest(toolFlags);
        }

        public void RAMTest(ICLIFlags toolFlags) {
            // how many streams can we load at once.
            // turns out we can load a lot and still be fine.
            
            List<Stream> streams = new List<Stream>();
            
            foreach (ulong key in TrackedFiles[0x4]) {
                Stream stream = OpenFile(key);
                MemoryStream memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                streams.Add(memoryStream);
            }
        }
    }
}