using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class DMCE : IChunk {
        public string Identifier => "DMCE"; // ECMD - Effect Child Model Data
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong modelKey;
            public ulong materialKey;
            public ulong animationKey;
            public ulong parentBone;  // todo: unsure, but 100% not frame
        }

        public Structure Data { get; private set; }

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();
            }
        }
    }
}
