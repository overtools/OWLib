using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace TankLib.Chunks {
    public class teEffectComponentModel : IChunk {
        public string ID => "ECMD";
        public List<IChunk> SubChunks { get; set; }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public teResourceGUID Model;
            public teResourceGUID ModelLook;
            public teResourceGUID Animation;
        }

        public Structure Header;

        public void Parse(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Header = reader.Read<Structure>();
            }
        }
    }
}