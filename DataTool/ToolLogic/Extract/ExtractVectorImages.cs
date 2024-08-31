using System;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;
using TankLib.Helpers;
using TankLib.STU.Types;
using static DataTool.Program;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-vector-images", Description = "Extracts vector images used in UI to SVGs", CustomFlags = typeof(ExtractFlags))]
    public class ExtractVectormages : ITool {
        private const string Container = "VectorImages";

        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            flags.EnsureOutputDirectory();

            var outputDir = Path.Combine(flags.OutputPath, Container);
            foreach (var guid in TrackedFiles[0x129]) {
                try {
                    SaveVectorImage(guid, outputDir);
                } catch (Exception ex) {
                    Logger.Error($"Failed to save vector image {teResourceGUID.AsString(guid)}: {ex.Message}");
                }
            }
        }

        private static void SaveVectorImage(ulong guid, string outputDir) {
            var vector = STUHelper.GetInstance<STU_AB64F377>(guid);
            if (vector == null) {
                return;
            }

            Logger.Log($"Saving vector image {teResourceGUID.AsString(guid)}");
            IO.CreateDirectorySafe(outputDir);
            var canvasPos = vector.m_A9942E29.m_position;
            var canvasSize = vector.m_A9942E29.m_09D545A3 + canvasPos;
            var foregrounds = vector.m_748D8791.Select(x => x.m_110634A1).ToArray();

            using var file = File.OpenWrite(Path.Combine(outputDir, $"{teResourceGUID.Index(guid):X12}.svg"));
            file.SetLength(0);

            using var writer = new StreamWriter(file);
            writer.Write($"<svg x=\"{canvasPos.X}\" y=\"{canvasPos.Y}\" viewBox=\"0 0 {canvasSize.X} {canvasSize.Y}\" xmlns=\"http://www.w3.org/2000/svg\">\n");

            for (var index = 0; index < vector.m_41B95C7C.Length; index++) {
                var path = vector.m_41B95C7C[index];
                var foreground = foregrounds[index];
                if (path.m_9557A9B0 == null || path.m_9557A9B0.Length == 0) {
                    continue;
                }

                writer.Write("\t<path d=\"");

                foreach (var triangle in path.m_9557A9B0 ?? Array.Empty<STU_D57A0ABB>()) {
                    var vertex1 = path.m_88FCECD7[triangle.m_636C5113].m_position;
                    var vertex2 = path.m_88FCECD7[triangle.m_F29D1FBF].m_position;
                    var vertex3 = path.m_88FCECD7[triangle.m_B30017DE].m_position;

                    writer.Write($"M {vertex1.X},{vertex1.Y} L {vertex2.X},{vertex2.Y} {vertex3.X},{vertex3.Y} Z ");
                }

                writer.Write("\" ");

                switch (foreground) {
                    case STU_06BD7A87 foregroundColor:
                        writer.Write($"style=\"fill:{foregroundColor.m_color.ToHex(false)};fill-opacity:{foregroundColor.m_AB865FDF};\" ");
                        break;
                    case STU_7654809A foregroundTexture:
                        // not used
                        break;
                }

                writer.Write("/>\n");
            }

            writer.Write("</svg>\n");
            writer.Flush();
        }
    }
}