using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.Chunk {
    public class PRHM : IChunk {
        public string Identifier => "PRHM"; // MHRP - Model Hard Points
        public string RootIdentifier => "LDOM"; // MODL - Model

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public int HardpointCount;
            public int UnkCount;
            public long HardpointOffset;
            public long UnkkOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HardPoint {
            public Matrix4B Matrix;
            public ulong HardPointGUID;
            public ulong GUIDx012;

            public ulong Unknown1;
            public ulong Unknown2;
        }

        public Structure Data { get; private set; }

        public HardPoint[] HardPoints { get; private set; }

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
                Data = reader.Read<Structure>();
                if (Data.HardpointCount > 0 && Data.HardpointOffset > 0) {
                    HardPoints = new HardPoint[Data.HardpointCount];
                    input.Position = Data.HardpointOffset;
                    for (uint i = 0; i < Data.HardpointCount; ++i) HardPoints[i] = reader.Read<HardPoint>();
                } else {
                    HardPoints = new HardPoint[0];
                }
            }
        }
    }
}