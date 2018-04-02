using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank ShaderInstance, file type 086</summary>
    public class teShaderInstance {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct TextureInputDefinition {
            /// <summary>Texture Type</summary>
            /// <remarks>Matches up with a teMaterialDataTexture on the MaterialData</remarks>
            public teShaderTextureType TextureType;
            
            /// <summary>Shader resource index</summary>
            public byte Index;
            
            public byte UnknownB;
            public byte UnknownC;
            public byte UnknownD;
            public short Zero;
            public short MinusOne;
        }
        
        /// <summary>ShaderInstance Header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct InstanceHeader {
            public long OffsetA;
            
            /// <summary>Offset to texture input defintions</summary>
            public long TextureInputOffset;
            
            public long OffsetC;
            public long OffsetD;
            public long OffsetE;
            
            /// <summary>teShaderCode reference</summary>
            /// <remarks>File type 087</remarks>
            public ulong ShaderCode;

            // these are just to get to the right position
            public ulong PadA;
            public ulong PadB;
            public ulong PadC;
            public uint PadD;

            public byte Unknown;
            
            /// <summary>Texture input definition count</summary>
            public sbyte TextureInputCount;  // wutface (does make sense tho, max textures is 128
        }

        /// <summary>Header data</summary>
        public InstanceHeader Header;
        
        /// <summary>Shader texture inputs</summary>
        public TextureInputDefinition[] TextureInputs;

        /// <summary>
        /// Read ShaderInstance from a stream
        /// </summary>
        /// <param name="stream">The stream to load from</param>
        public teShaderInstance(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Read(reader);
            }
        }

        /// <summary>
        /// Read ShaderInstance from a BinaryReader
        /// </summary>
        /// <param name="reader">The reader to load from</param>
        public teShaderInstance(BinaryReader reader) {
            Read(reader);
        }

        private void Read(BinaryReader reader) {
            Header = reader.Read<InstanceHeader>();
            
            if (Header.TextureInputOffset != 0 && Header.TextureInputCount > -1) {
                reader.BaseStream.Position = Header.TextureInputOffset;

                TextureInputs = reader.ReadArray<TextureInputDefinition>(Header.TextureInputCount);
            }
        }
    }
}