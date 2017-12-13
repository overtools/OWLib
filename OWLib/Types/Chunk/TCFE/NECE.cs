using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class NECE : IChunk {
        public string Identifier => "NECE"; // ECEN - Effect Chunk Entity
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong Entity;
            public ulong EntityVariable;
        }

        public Structure Data { get; private set; }

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();
            }
        }
    }
}
