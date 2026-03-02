using System;
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
using TankLib.Helpers;
using TankLib.STU.Types.Enums;
using static DataTool.Program;

namespace DataTool.ToolLogic.Dump;

[Tool("dump-ui-textures", Description = "Saves all UI textures", CustomFlags = typeof(ExtractFlags))]
public class DumpUiTextures : JSONTool, ITool {
    public void Parse(ICLIFlags toolFlags) {
        var flags = (ExtractFlags) toolFlags;
        var basePath = flags.OutputPath;

        var guids = new ConcurrentBag<ulong>();

        AnsiConsole.Progress().Start(ctx => {
            var task = ctx.AddTask("Finding textures", true, TrackedFiles[0x4].Count);

            // Search in parallel to speed up discovery
            Parallel.ForEach(TrackedFiles[0x4], new ParallelOptions {
                MaxDegreeOfParallelism = IO.GetParallelismAmount(4),
            }, guid => {
                task.Increment(1);

                try {
                    var file = IO.OpenFile(guid);
                    if (file == null) return;
                    var texture = new teTexture(file);

                    // ui category or something
                    if (texture.Header.UsageCategory is Enum_950F7205.xD5BCD1B2 or Enum_950F7205.x25763F45) {
                        guids.Add(guid);
                    }
                } catch (Exception ex) {
                    Logger.Warn($"Failed to reading texture {guid:X16}: {ex.Message}");
                }
            });

            task.StopTask();
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

            Parallel.ForEach(info.m_textures.Values, new ParallelOptions {
                MaxDegreeOfParallelism = IO.GetParallelismAmount(4),
            }, textureInfo => {
                task.Increment(1);
                if (!textureInfo.m_loose) {
                    return;
                }

                SaveLogic.Combo.SaveTexture(flags, outputPath, saveContext, textureInfo.m_GUID, saveOptions);
            });

            task.StopTask();
        });
    }
}