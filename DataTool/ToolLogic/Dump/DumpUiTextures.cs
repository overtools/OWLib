using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using DataTool.ToolLogic.Extract;
using Spectre.Console;
using TankLib;
using TankLib.STU.Types.Enums;
using static DataTool.Program;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-ui-textures", Description = "Saves all UI textures", CustomFlags = typeof(ExtractFlags))]
    public class DumpUiTextures : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            var basePath = flags.OutputPath;

            Log("Finding textures...");
            var guids = new ConcurrentBag<ulong>();

            // Search in parallel to speed up discovery
            Parallel.ForEach(TrackedFiles[0x4], new ParallelOptions {
                MaxDegreeOfParallelism = 3,
            }, guid => {
                var file = IO.OpenFile(guid);
                if (file == null) return;
                var texture = new teTexture(file);

                // ui category or something
                if (texture.Header.UsageCategory == Enum_950F7205.xD5BCD1B2) {
                    guids.Add(guid);
                }
            });

            Combo.ComboInfo info = new Combo.ComboInfo();
            foreach (var guid in guids) {
                Combo.Find(info, guid);
            }

            Log($"Preparing to save roughly {info.m_textures.Count} textures.");
            var saveContext = new SaveLogic.Combo.SaveContext(info);
            var outputPath = Path.Combine(basePath, "UITextureDump");
            var saveOptions = new SaveLogic.Combo.SaveTextureOptions {
                ProcessIcon = true,
            };

            AnsiConsole.Progress().Start(ctx => {
                var task = ctx.AddTask("Saving textures", true, info.m_textures.Values.Count);

                foreach (var textureInfo in info.m_textures.Values) {
                    task.Increment(1);
                    if (!textureInfo.m_loose) continue;
                    SaveLogic.Combo.SaveTexture(flags, outputPath, saveContext, textureInfo.m_GUID, saveOptions);
                }

                task.StopTask();
            });
        }
    }
}