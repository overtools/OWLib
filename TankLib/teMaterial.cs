using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank Material, file type 008</summary>
    public class teMaterial {
        /// <summary>Material header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MatHeader {
            /// <summary>teShaderGroup reference</summary>
            /// <remarks>File type 085</remarks>
            public teResourceGUID ShaderGroup;
            
            /// <summary>teShaderSource reference</summary>
            /// <remarks>File type 088, mostly virtual</remarks>
            public teResourceGUID ShaderSource;
            
            /// <summary>Unknown reference</summary>
            /// <remarks>File type 03A</remarks>
            public teResourceGUID GUIDx03A;
            
            /// <summary>teMaterialData reference</summary>
            /// <remarks>File type 0B3</remarks>
            public teResourceGUID MaterialData;
        }

        /// <summary>Header data</summary>
        public MatHeader Header;

        /// <summary>Load material from a stream</summary>
        public teMaterial(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Header = reader.Read<MatHeader>();
            }
        }
    }
}