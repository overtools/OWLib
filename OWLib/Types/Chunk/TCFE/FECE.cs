using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class FECE : IChunk {
        public string Identifier => "FECE"; // ECEF - Effect Chunk Effect
        public string RootIdentifier => "TCFE"; // EFCT - Effect
    
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong Effect;
            public byte unk1;
            public byte unk2;
            public byte unk3;
            public byte unk4;
            public uint unk5;
        }

        public Structure Data { get; private set; }

        public void Parse(Stream input) {
            using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();
            }
        }
    }
}
