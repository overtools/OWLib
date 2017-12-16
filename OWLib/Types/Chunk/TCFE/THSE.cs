using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class THSE : IChunk {
        public string Identifier => "THSE"; // ECHT - Effect ?????
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong Hardpoint;
        }

        public Structure Data { get; private set; }

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();
            }
        }
    }
}
