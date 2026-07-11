using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using TankLib.Helpers.DataSerializer;

namespace TankLib {
    /// <summary>Tank ShaderCode, file type 087</summary>
    public class teShaderCode {
        /// <summary>ShaderCode Header </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct ShaderCodeHeader {
            /// <summary>Shader type flags</summary>
            [FieldOffset(8)] public Enums.teSHADER_TYPE ShaderType; // 36 -> 8
            /// <summary>Offset to data</summary>
            [FieldOffset(112)] public long DataOffset; // 0 -> 112
            /// <summary>Size of compressed DXBC data</summary>
            [FieldOffset(132)] public int CompressedSize; // 24 -> 132
            /// <summary>Size of decompressed DXBC data</summary>
            [FieldOffset(136)] public int UncompressedSize; // 28 -> 136
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