using System;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.ToolLogic.Extract.Debug;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-abilities", Description = "Extract abilities", CustomFlags = typeof(ExtractFlags))]
    public class ExtractAbilities : ITool {
        public void Parse(ICLIFlags toolFlags) {
            SaveAbilities(toolFlags);
        }

        public static void SaveAbilities(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            const string folderName = "Abilities";

            foreach (ulong key in TrackedFiles[0x9E]) {
                STULoadout loadout = GetInstance<STULoadout>(key);
                if (loadout == null) continue;

                string name = GetValidFilename(GetCleanString(loadout.m_name)?.TrimEnd().Replace(".", "_")) ?? $"Unknown{teResourceGUID.Index(key):X}";
                var directory = Path.Combine(basePath, folderName, name);

                Combo.ComboInfo info = new Combo.ComboInfo();
                Combo.Find(info, loadout.m_texture);

                var context = new SaveLogic.Combo.SaveContext(info);
                SaveLogic.Combo.SaveLooseTextures(flags, directory, context);

                ExtractDebugMovies.SaveVideoFile(flags, loadout.m_infoMovie, directory);
            }
        }
    }
}
