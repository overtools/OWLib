using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class CECE : IChunk {
        public string Identifier => "CECE"; // ECEC - Effect Chunk Entity Control
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        public enum CECEAction : byte {  // todo: maybe u16/i16
            Show = 1,  // Animation is 0
            PlayAnim = 4
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong Animation;
            public ulong EntityVariable;
            public CECEAction Action;
        }

        public Structure Data { get; private set; }

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();
            }
        }
    }
}
