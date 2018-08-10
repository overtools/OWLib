using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank ShaderCode, file type 087</summary>
    public class teShaderCode {
        /// <summary>ShaderCode Header </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ShaderCodeHeader {
            /// <summary>Offset to DXBC data</summary>
            public long DataOffset; // 0
            
            /// <summary>Offset to unknown data</summary>
            public long OffsetB; // 8
            
            public uint UnknownFF; // 16
            
            /// <summary>Unknown data count</summary>
            public uint StreamOutDescCount; // 20
            
            public uint Unknown1; // 24
            
            /// <summary>Size of DXBC data</summary>
            public int DataSize;  // 28
            
            /// <summary>Shader type</summary>
            public Enums.teSHADER_TYPE ShaderType;  // 32

            public uint Unknown2;  // maybe hash or something. sometimes 0
        }

        /// <summary>Header Data</summary>
        public ShaderCodeHeader Header;
        
        /// <summary>Compiled DXBC Data</summary>
        public byte[] Data;
        
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
            
            if (Header.DataOffset >= 0) {
                reader.BaseStream.Position = Header.DataOffset;
                Data = reader.ReadBytes(Header.DataSize);
            }
        }
    }
}