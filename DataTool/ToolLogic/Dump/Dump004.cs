using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.JSON;
using DataTool.ToolLogic.Extract;
using Spectre.Console;
using static DataTool.Program;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-textures", Description = "Saves all textures", CustomFlags = typeof(ExtractFlags))]
    public class DumpTextures : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            var basePath = flags.OutputPath;

            Combo.ComboInfo info = new Combo.ComboInfo();

            foreach (ulong key in TrackedFiles[0x4]) {
                Combo.Find(info, key);
            }

            Log($"Preparing to save roughly {info.m_textures.Count} textures.");
            Log($"This will take a long time and take up a lot of space.");

            var saveContext = new SaveLogic.Combo.SaveContext(info);
            var outputPath = Path.Combine(basePath, "TextureDump");

            AnsiConsole.Progress().Start(ctx => {
                var task = ctx.AddTask("Saving textures", true, info.m_textures.Values.Count);

                foreach (var textureInfo in info.m_textures.Values) {
                    task.Increment(1);
                    if (!textureInfo.m_loose) continue;
                    SaveLogic.Combo.SaveTexture(flags, outputPath, saveContext, textureInfo.m_GUID);
                }

                task.StopTask();
            });
        }
    }
}