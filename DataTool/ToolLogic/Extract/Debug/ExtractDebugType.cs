using System;
using System.IO;
using DataTool.Flag;
using static DataTool.Program;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-type", Description = "Extract type (debug)", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugType : ITool {
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

            const string container = "DebugTypes";
            string path = Path.Combine(basePath, container);
            
            WriteType(0x88, path);
            //WriteType(0x43, path);
        }

        public void WriteType(ushort type, string path) {
            string thisPath = Path.Combine(path, type.ToString("X3"));
            foreach (ulong @ulong in TrackedFiles[type]) {
                WriteFile(@ulong, thisPath);
            }
        }
    }
}