using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using DataTool.Flag;
using DataTool.JSON;
using DataTool.ToolLogic.Extract;
using TankLib;
using static DataTool.Program;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-file-lists", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class DumpFileLists : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            if (flags.OutputPath == null)
                throw new Exception("no output path");

            var outputPath = Path.Combine(flags.OutputPath, "filelists");
            CreateDirectorySafe(outputPath);
           
            foreach (KeyValuePair<ushort,HashSet<ulong>> type in TrackedFiles) {
                var outputFile = $"{Path.Combine(outputPath, type.Key.ToString("X3"))}.txt";
                var files = type.Value.OrderBy(x => x).Select(teResourceGUID.AsString);
                File.WriteAllLines(outputFile, files);
            }
        }
    }
}