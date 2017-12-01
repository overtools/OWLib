using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class TCFE : IChunk {
        public string Identifier => "TCFE"; // ECEC - Effect
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {  // rough struct so I can get to the data I need
            public long Unknown1;
            public long Unknown2;
            public long Unknown3;
            public long Unknown4;
            public long Unknown5;
            public long Unknown6;
            public float EndTime1;
            public float EndTime2;
            public float MaxPercentage1; // todo: verify
            public float MaxPercentage2; // todo: verify
        }

        public Structure Data { get; private set; }

        public const int FPS = 30;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();
            }
        }
    }
}
