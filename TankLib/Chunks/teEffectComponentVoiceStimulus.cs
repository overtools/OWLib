using System.IO;
using System.Runtime.InteropServices;

namespace TankLib.Chunks {
    public class teEffectComponentVoiceStimulus : IChunk {
        public string ID => "ECVS";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong VoiceStimulus;
        }

        public Structure Header;

        public void Parse(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Header = reader.Read<Structure>();
            }
        }
    }
}