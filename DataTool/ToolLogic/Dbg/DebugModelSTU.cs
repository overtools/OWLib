using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Extract;
using TankLib;
using TankLib.Chunks;
using TankLib.STU.Types;

namespace DataTool.ToolLogic.Dbg {
    [Tool("te-model-chunk-stu", Description = "", TrackTypes = new ushort[] {0xC}, IsSensitive = true, CustomFlags = typeof(ExtractFlags))]
    class DebugModelSTU : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            var flags = toolFlags as ExtractFlags;
            var testGuids = flags?.Positionals.Skip(3).Select(x => uint.Parse(x, System.Globalization.NumberStyles.HexNumber));
            foreach (var guid in Program.TrackedFiles[0xC]) {
                if (!(testGuids ?? throw new InvalidDataException()).Contains(teResourceGUID.Index(guid))) continue;
                using (Stream file = IO.OpenFile(guid))
                using (BinaryReader reader = new BinaryReader(file)) {
                    teChunkedData chunk = new teChunkedData(reader);
                    teModelChunk_STU stuChunk = chunk.GetChunk<teModelChunk_STU>();

                    var hitboxes = stuChunk.StructuredData.m_CB4D298D;
                    var complex = hitboxes.Select(x => x.m_B7C8314A).OfType<STU_B3800E70>().First();
                    var lines = new List<string> {
                                                     "ply",
                                                     "format ascii 1.0",
                                                     $"element vertex {complex.m_88FCECD7.Length}",
                                                     "property float x",
                                                     "property float y",
                                                     "property float z",
                                                     "end_header"
                                                 };
                    lines.AddRange(complex.m_88FCECD7.Select(x => $"{x.X} {x.Y} {x.Z}")); // vertex

                    File.WriteAllText(@"F:\Test.ply", string.Join("\n", lines));
                }
            }
        }
    }
}
