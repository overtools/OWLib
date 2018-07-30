using System.IO;
using System.Runtime.InteropServices;

namespace TankLib.Chunks {
    public class teEffectComponentParticle : IChunk {
        public string ID => "ECPR";
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public ulong Unknown1;
            public ulong Unknown2;
            public ulong Unknown3;
            public ulong Unknown4;
            public ulong Model;
        }

        public Structure Header;

        public void Parse(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Header = reader.Read<Structure>();
            }
        }
    }
}