using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class TACE : IChunk {
        public string Identifier => "TACE"; // ECHT - Effect Chunk ?????
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong GUIDx03C;
        }

        public Structure Data { get; private set; }

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();
            }
        }
    }
}
