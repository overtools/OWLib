using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.ToolLogic.Render {
    [Tool("render-vectors", Description = "Render vector graphics", CustomFlags = typeof(RenderFlags), IsSensitive = true)]
    public class RenderVectors : ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (RenderFlags) toolFlags;
            var output = Path.Combine(flags.OutputPath, "Vector");
            if (!Directory.Exists(output)) {
                Directory.CreateDirectory(output);
            }

            foreach (var type in new ushort[] {0x129}) {
                if (!Directory.Exists(Path.Combine(output, type.ToString("X3")))) {
                    Directory.CreateDirectory(Path.Combine(output, type.ToString("X3")));
                }

                foreach (var guid in Program.TrackedFiles[type]) {
                    using var stu = STUHelper.OpenSTUSafe(guid);
                    var vector = stu.GetMainInstance<STU_AB64F377>();

                    var canvasPos = vector.m_A9942E29.m_position;
                    var canvasSize = vector.m_A9942E29.m_09D545A3 + canvasPos;

                    var foregrounds = vector.m_748D8791.Select(x => x.m_110634A1).ToArray(); // STU_06BD7A87 = color, STU_7654809A = texture
                    var backgrounds = vector.m_748D8791.Select(x => x.m_C7E229EE).ToArray();

                    using var file = File.OpenWrite(Path.Combine(output, $"{teResourceGUID.Index(guid):X12}.svg"));
                    file.SetLength(0);
                    using var writer = new StreamWriter(file);
                    writer.Write($"<svg x=\"{canvasPos.X}\" y=\"{canvasPos.Y}\" width=\"{canvasSize.X}\" height=\"{canvasSize.Y}\" xmlns=\"http://www.w3.org/2000/svg\">\n");

                    for (var index = 0; index < vector.m_41B95C7C.Length; index++) {
                        var path = vector.m_41B95C7C[index];
                        if (path.m_88FCECD7 == null) {
                            continue;
                        }
                        var pathPos = path.m_18789D20.m_position - canvasPos;
                        var pathSize = path.m_18789D20.m_09D545A3 + pathPos + canvasPos;
                        var foreground = foregrounds[index];
                        var stroke = backgrounds[index];
                        var order = path.m_9C117710;
                        var pathData = path.m_88FCECD7.Select(x => x.m_position).ToArray();
                        var groups = path.m_CFE03E77;

                        writer.Write($"<svg x=\"{pathPos.X}\" y=\"{pathPos.Y}\" width=\"{pathSize.X}\" height=\"{pathSize.Y}\" >\n");
                        foreach (var triangle in path.m_9557A9B0) {
                            var position1 = pathData[triangle.m_636C5113];
                            var position2 = pathData[triangle.m_F29D1FBF];
                            var position3 = pathData[triangle.m_B30017DE];
                            writer.Write($"<polygon points=\"{position1.X},{position1.Y} {position2.X},{position2.Y} {position3.X},{position3.Y}\" ");
                            switch (foreground) {
                                case STU_06BD7A87 foregroundColor:
                                    writer.Write($" fill=\"{foregroundColor.m_color.ToCSS()}\" ");
                                    break;
                                case STU_7654809A foregroundTexture:
                                    // todo: texture
                                    break;
                            }

                            switch (stroke) {
                                case STU_06BD7A87 strokeColor:
                                    writer.Write($" stroke=\"{strokeColor.m_color.ToCSS()}\" ");
                                    break;
                                case STU_7654809A strokeTexture:
                                    // todo: texture
                                    break;
                            }

                            writer.Write("/>\n");
                        }
                        // for (ushort i = 0; i < groups.Length; i++) {
                        //     // what is this enum and flag lol
                        //     var group = groups[i];
                        //     var start = group.m_550D19E2;
                        //     if (i > 0 && group.m_flags == 1) {
                        //         start += i;
                        //     }
                        //     var end = i < groups.Length - 1 ? groups[i + 1].m_550D19E2 : (ushort) order.Length;
                        //     writer.Write("<polygon points=\"");
                        //     for (; start < end; start++) {
                        //         var position = pathData[order[start]];
                        //         writer.Write($"{position.X},{position.Y} ");
                        //     }
                        //
                        //     writer.Write("\" style=\"stroke-width:1;");
                        //
                        //     switch (foreground) {
                        //         case STU_06BD7A87 foregroundColor:
                        //             writer.Write($"fill:{foregroundColor.m_color.ToCSS()};");
                        //             break;
                        //         case STU_7654809A foregroundTexture:
                        //             // todo: texture
                        //             break;
                        //     }
                        //
                        //     switch (stroke) {
                        //         case STU_06BD7A87 strokeTexture:
                        //             writer.Write($"stroke:{strokeTexture.m_color.ToCSS()};");
                        //             break;
                        //         case STU_7654809A strokeTexture:
                        //             // todo: texture
                        //             break;
                        //     }
                        //
                        //     writer.Write("\"/>\n");
                        // }

                        writer.Write("</svg>\n");
                    }
                    writer.Write("</svg>\n");
                }
            }
        }
    }
}
