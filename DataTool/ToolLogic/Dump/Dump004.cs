using System;
using System.IO;
using System.Linq;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.JSON;
using DataTool.ToolLogic.Extract;
using static DataTool.Program;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-004", Description = "Dumps 004", CustomFlags = typeof(ExtractFlags))]
    public class Dump004 : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            Combo.ComboInfo info = new Combo.ComboInfo();
            
            foreach (ulong key in TrackedFiles[0x4]) {
                Combo.Find(info, key);
            }

            Log($"Preparing to save roughly {info.Textures.Count()} textures.");
            Log($"This will take a long time and take up a lot of space.");
            SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(basePath, "004Dump"), info);
        }
    }
}