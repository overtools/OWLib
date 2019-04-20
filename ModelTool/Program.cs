using System.IO;
using System.Linq;
using TankLib;
using TankLib.ExportFormats;
using TankLib.Helpers;

namespace ModelTool {
    /// <summary>
    /// It's back baybeee
    /// </summary>
    public class Program {
        public static void Main(string[] args) {
            var filelist = args.ElementAtOrDefault(0);
            var src = args.ElementAtOrDefault(1);
            var dst = args.ElementAtOrDefault(2);

            if (string.IsNullOrWhiteSpace(filelist)) return;


            if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(dst)) {
                if (!File.Exists(filelist)) {
                    return;
                }

                var chunkedData = new teChunkedData(File.OpenRead(filelist));
                var model = new OverwatchModel(chunkedData, 10, 1);
                var dstFile = Path.ChangeExtension(filelist, model.Extension);
                using (var modelFile = File.OpenWrite(dstFile)) {
                    model.Write(modelFile);
                }
            } else {
                var files = File.ReadAllLines(filelist);

                if (!Directory.Exists(dst)) {
                    Directory.CreateDirectory(dst);
                }

                foreach (var file in files) {
                    var filename = Path.Combine(src, file);
                    if (!File.Exists(filename)) {
                        Logger.Error("EXPORT", file);
                        continue;
                    }

                    var chunkedData = new teChunkedData(File.OpenRead(filename));
                    var model = new OverwatchModel(chunkedData, 10, 1);
                    var dstFile = Path.Combine(dst, Path.ChangeExtension(file, model.Extension));
                    using (var modelFile = File.OpenWrite(dstFile)) {
                        model.Write(modelFile);
                    }
                }
            }
        }
    }
}
