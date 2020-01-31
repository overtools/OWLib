using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using TankLib.Helpers.DataSerializer;

namespace TankLib {
    /// <summary>Tank ShaderCode, file type 087</summary>
    public class teShaderCode {
        /// <summary>ShaderCode Header </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ShaderCodeHeader {
            /// <summary>Offset to data</summary>
            public long DataOffset; // 0
            
            /// <summary>Offset to unknown data</summary>
            public long OffsetB; // 8
            
            public uint UnknownFF; // 16
            
            /// <summary>Unknown data count</summary>
            public uint StreamOutDescCount; // 20
            
            /// <summary>Size of compressed DXBC data</summary>
            public int CompressedSize;  // 24

            /// <summary>Size of decompressed DXBC data</summary>
            public int UncompressedSize;  // 28

            public uint Unknown; // 32
            
            /// <summary>Shader type</summary>
            public Enums.teSHADER_TYPE ShaderType;  // 36
            
            // etc
        }

        /// <summary>Header Data</summary>
        public ShaderCodeHeader Header;

        /// <summary>Shader DXBC Byte Code</summary>
        public byte[] ByteCode;
        
        /// <summary>
        /// Read ShaderCode from a stream
        /// </summary>
        /// <param name="stream">Stream to load from</param>
        public teShaderCode(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Read(reader);
            }
        }

        /// <summary>
        /// Read ShaderCode from a BinaryReader
        /// </summary>
        /// <param name="reader">The reader to load from</param>
        public teShaderCode(BinaryReader reader) {
            Read(reader);
        }

        private void Read(BinaryReader reader) {
            Header = reader.Read<ShaderCodeHeader>();
            reader.BaseStream.Position = Header.DataOffset;
            using (GZipStream gzip = new GZipStream(reader.BaseStream, CompressionMode.Decompress)) {
                ByteCode = new byte[Header.UncompressedSize];
                gzip.Read(ByteCode, 0, ByteCode.Length);
            }
        }
    }
}