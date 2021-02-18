using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace TankLib.Chunks {
    // ReSharper disable once InconsistentNaming
    public class teEffect_CAT : IChunk {
        public string ID => "ECAT";
        public List<IChunk> SubChunks { get; set; }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public teResourceGUID Hardpoint;
        }

        public Structure Header;

        public void Parse(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Header = reader.Read<Structure>();
            }
        }
    }
}