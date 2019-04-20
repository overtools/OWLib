using System.IO;
using System.Linq;
using TankLib;
using TankLib.ExportFormats;
using TankLib.Helpers;

namespace TextureTool {
    /// <summary>
    /// It's back baybeee
    /// </summary>
    public class Program {
        public static void Main(string[] args) {
            var filelist = args.ElementAtOrDefault(0);
            var src = args.ElementAtOrDefault(1);
            var payloadDir = args.ElementAtOrDefault(2);
            var dst = args.ElementAtOrDefault(2);

            if (string.IsNullOrWhiteSpace(filelist)) return;

            if (string.IsNullOrWhiteSpace(payloadDir)) {
                if (!File.Exists(filelist)) {
                    return;
                }
                var target = Path.ChangeExtension(filelist, "dds");
                var texture = new teTexture(File.OpenRead(filelist));
                if (src != null && File.Exists(src)) {
                    try {
                        texture.LoadPayload(File.OpenRead(src));
                    } catch {
                        // ignored
                    }
                }

                texture.SaveToDDS(File.OpenWrite(target));
            } else if(!string.IsNullOrWhiteSpace(src)) {
                var files = File.ReadAllLines(filelist);

                if (!Directory.Exists(payloadDir)) {
                    Directory.CreateDirectory(payloadDir);
                }

                foreach (var file in files) {
                    var filename = Path.Combine(src, file);
                    if (!File.Exists(filename)) {
                        Logger.Error("EXPORT", file);
                        continue;
                    }
                    
                    var payload = Path.Combine(payloadDir, Path.ChangeExtension(Path.GetFileName(file), "04D"));
                    var target = Path.ChangeExtension(filename, "dds");
                    var texture = new teTexture(File.OpenRead(filename));
                    if (File.Exists(payload)) {
                        try {
                            texture.LoadPayload(File.OpenRead(payload));
                        } catch {
                            // ignored
                        }
                    }

                    texture.SaveToDDS(File.OpenWrite(target));
                }
            }
        }
    }
}
