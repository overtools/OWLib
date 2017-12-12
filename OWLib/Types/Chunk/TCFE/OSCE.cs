using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class OSCE : IChunk {
        public string Identifier => "OSCE"; // ECSO - Effect Chunk ??????
        public string RootIdentifier => "TCFE"; // EFCT - Effect
    
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong soundDataKey;  // so this isn't an effect(?)
            public ulong unk1;
            public ulong unk2;
            public ulong unk3;
        }

        public Structure Data { get; private set; }

        public void Parse(Stream input) {
            using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();
            }
        }
    }
}   
