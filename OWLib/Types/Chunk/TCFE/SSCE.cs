using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class SSCE : IChunk {
        public string Identifier => "SSCE"; // ECSS - Effect Shaders?
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        [StructLayout(LayoutKind.Sequential, Pack = 4)]  // this doesn't work anymore
        public struct Structure {
            public ulong unknown1;
            public ulong material_key;
            public ulong definition_key;
        }

        public Structure Data { get; private set; }

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();
            }
        }
    }
}
