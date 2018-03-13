using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank Material, file type 008</summary>
    public class teMaterial {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MatHeader {
            public teResourceGUID ShaderGroup;
            public teResourceGUID ShaderSource;
            public teResourceGUID GUIDx03A;
            public teResourceGUID MaterialData;
        }

        public MatHeader Header;

        /// <summary>Load material from a stream</summary>
        public teMaterial(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Header = reader.Read<MatHeader>();
            }
        }
    }
}