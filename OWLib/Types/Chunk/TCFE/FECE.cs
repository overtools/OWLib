using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class FECE : IChunk {
        public string Identifier => "FECE"; // FECE - ?
        public string RootIdentifier => "TCFE"; // EFCT - Effect
    
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong effect;
            public byte unk1;
            public byte unk2;
            public byte unk3;
            public byte unk4;
            public uint unk5;
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
