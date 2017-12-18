using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class EffectChunkComponent : IChunk {
        public string Identifier => "PMCE"; // PMCE - teEffectChunkComponent
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {  // this is messy and only just works
            public long StartTimeOffset;
            public long EndTimeOffset;
            public ulong Hardpoint;
            public float FloatOneA;
            public float FloatOneB;
            public long Unk;
            public short Index;

            // public short ShortFlag;
            // public short MinusOneShort;

            // public float StartTime; // no

            // public long Pad1;
            // public long Pad2;
            // public int Pad3;

            // public PMCEType Flag; // hmm
            // public short Unk3;
            // public short Unk4;
            // public short Unk5;
            // public short Unk6;
        }

        public Structure Data { get; private set; }
        public float StartTime;
        public float EndTime;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();
                
                // research notes:
                // 000000002206.08F:
                //     end = frame 150 = 5 seconds = 1.0
                //     'GunFlash' = ECEF, start time stored on A
                //     'GunFlash' on frame 14:
                //         14/30 = 0.4666666666666
                //     'GunFlash' on frame 25:
                //         25/30 = 0.8333333333333333

                if (Data.StartTimeOffset != 0) {
                    reader.BaseStream.Position = Data.StartTimeOffset;
                    StartTime = reader.ReadSingle();
                }
                if (Data.EndTimeOffset != 0) {
                    reader.BaseStream.Position = Data.EndTimeOffset;
                    EndTime = reader.ReadSingle();
                }
            }
        }
    }
}
