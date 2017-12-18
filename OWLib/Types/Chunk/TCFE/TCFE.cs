using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class TCFE : IChunk {
        public string Identifier => "TCFE"; // ECEC - Effect
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        [StructLayout(LayoutKind.Sequential, Pack = 1)]  // erm
        public struct Structure {  // rough struct
            public long Offset1;
            public long Offset2;
            public long Offset3;
            public long HardpointArrayOffset;
            public long Offset5;  // hmm
            public long Offset6;  // hmm
            public float EndTime1;
            public float EndTime2;
            public float MaxPercentage1; // todo: verify
            public float MaxPercentage2; // todo: verify
            
            public int Unk;
            public int Unk2;
            public short Unk3;
            public short HardpointArrayCount;
        }

        public Structure Data { get; private set; }
        public ulong[] Hardpoints;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();

                if (Data.HardpointArrayOffset != 0 && Data.HardpointArrayCount >= 0) {
                    reader.BaseStream.Position = Data.HardpointArrayOffset;
                    Hardpoints = new ulong[Data.HardpointArrayCount];
                    for (int i = 0; i < Data.HardpointArrayCount; i++) {
                        Hardpoints[i] = reader.ReadUInt64();
                    }
                }
            }
        }
    }
}
