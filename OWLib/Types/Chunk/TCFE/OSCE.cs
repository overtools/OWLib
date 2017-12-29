using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class OSCE : IChunk {
        public string Identifier => "OSCE"; // ECSO - Effect Chunk Sound ??
        public string RootIdentifier => "TCFE"; // EFCT - Effect
    
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong Sound;
            public ulong Unknown1;
            public short Unknown2;
            public ulong Unknown3;
        }

        public Structure Data { get; private set; }

        public void Parse(Stream input) {
            using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();
            }
        }
    }
}   
