using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class SVCE : IChunk {
        public string Identifier => "SVCE"; // ECVS - Effect Chunk Voice Stimulus
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong VoiceStimulus;
            public int Zero;
            public uint Unknown;
        }

        public Structure Data { get; private set; }

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();
            }
        }
    }
}
