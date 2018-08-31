using System;
using System.IO;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;

namespace DataTool.ToolLogic.Dbg {
    [Tool("brrap", Description = "I hear da call", TrackTypes = new ushort[] {0x5E}, IsSensitive = true)]
    class DebugVoodoo : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            foreach (var guid in Program.TrackedFiles[0x5E]) {
                using (Stream f = File.OpenWrite($@"C:\Overwatch\05E\{teResourceGUID.AsString(guid)}"))
                using (Stream d = IO.OpenFile(guid)) {
                    d.CopyTo(f);
                }

                if (teResourceGUID.Index(guid) == 0x397) {
                    System.Diagnostics.Debugger.Break();
                }
                
                var stu = STUHelper.OpenSTUSafe(guid);
            }
        }
    }
}
