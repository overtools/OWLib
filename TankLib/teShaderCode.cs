using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank ShaderCode, file type 087</summary>
    public class teShaderCode {
        /// <summary>ShaderCode Header </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ShaderCodeHeader {
            /// <summary>Offset to DXBC data</summary>
            public long DataOffset;
            
            /// <summary>Offset to unknown data</summary>
            public long OffsetB;
            
            public uint UnknownFF;
            
            /// <summary>Unknown data count</summary>
            public uint CountB;
            
            public uint Unknown;
            
            /// <summary>Size of DXBC data</summary>
            public int DataSize;
            
            /// <summary>Shader type</summary>
            public teEnums.teSHADER_TYPE ShaderType;
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