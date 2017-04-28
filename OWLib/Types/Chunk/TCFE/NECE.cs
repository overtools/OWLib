using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class NECE : IChunk {
        public string Identifier => "NECE"; // ECMD - Effect Child Model Data
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong key;
            public ulong unknown;
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
