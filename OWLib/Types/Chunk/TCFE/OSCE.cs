using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class OSCE : IChunk {
        public string Identifier => "OSCE"; // ESCO - ?
        public string RootIdentifier => "TCFE"; // EFCT - Effect
    
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong effect;
            public ulong unk1;
            public ulong unk2;
            public ulong unk3;
        }

        private Structure data;
        public Structure Data => data;

        public void Parse(Stream input) {
            using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                data = reader.Read<Structure>();
            }
        }
    }
}
