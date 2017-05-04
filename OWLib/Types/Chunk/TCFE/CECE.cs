using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class CECE : IChunk {
        public string Identifier => "CECE"; // ECEC - ????
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong animation;
            public ulong unknown_key;
        }

        private Structure data;
        public Structure Data => data;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                data = reader.Read<Structure>();
            }
        }
    }
}
