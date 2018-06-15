using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank ShaderInstance, file type 086</summary>
    public class teShaderInstance {
        /// <summary>ShaderInstance Header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct InstanceHeader {
            public long BufferOffset;
            
            /// <summary>Offset to texture input defintions</summary>
            public long TextureInputOffset;
            
            public long OffsetC;
            
            /// <summary>Offset to buffer input defintions</summary>
            public long UnknownInputOffset;
            
            /// <summary>Offset to vertex layut definition</summary>
            public long VertexLayoutOffset;
            
            /// <summary>teShaderCode reference</summary>
            /// <remarks>File type 087</remarks>
            public teResourceGUID ShaderCode;

            // these are just to get to the right position
            public ulong PadA;
            public ulong PadB;
            public ulong PadC;
            public uint PadD;

            public sbyte BufferCount;
            
            /// <summary>Texture input definition count</summary>
            public sbyte TextureInputCount;  // wutface (does make sense tho, max textures is 128
            
            public sbyte Unknown2;
            public sbyte UnknownCount;
            
            public sbyte VertexInputElementCount;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct TextureInputDefinition {
            /// <summary>Texture Type</summary>
            /// <remarks>Matches up with a teMaterialDataTexture on the MaterialData</remarks>
            public uint NameHash;
            
            /// <summary>Shader resource index</summary>
            public byte Index;
            
            public byte UnknownB;
            public byte UnknownC;
            public byte UnknownD;
            public short Zero;
            public short MinusOne;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct UnknownInputDefinition {
            /// <summary>Texture Type</summary>
            /// <remarks>Matches up with a teMaterialDataConstantBuffer on the MaterialData</remarks>
            public uint NameHash;
            
            /// <summary>Shader resource index</summary>
            public byte Index;
            public byte Unknown;
            public short MinusOne;
        }

        public enum InputElementType : byte {
            Position = 0,
            Normal = 1,
            Binormal = 2,
            Tangent = 3,
            BlendIndices = 4,
            BlendWeights = 5,
            Color = 8,
            TexCoord = 9,
            InstanceData = 15,
            VertexIndex = 16
        }

        public enum InputElementFormat : short {
            Float2 = 1,
            Float3 = 2,
            Float4 = 3,
            ColorRGBA = 8,
            UInt = 12,
            UInt4 = 13,
            Int4 = 16,
            UInt2 = 17
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct InputElementDefinitinon {
            public InputElementType ElementType;
            public byte Index;
            public InputElementFormat Format;
            public uint Unknown;
        }

        /// <summary>Header data</summary>
        public InstanceHeader Header;
        
        /// <summary>Shader texture inputs</summary>
        public TextureInputDefinition[] TextureInputs;
        
        /// <summary>Shader constant buffer inputs</summary>
        public UnknownInputDefinition[] UnknownInputs;

        /// <summary>Shader vertex layout</summary>
        public InputElementDefinitinon[] VertexLayout;

        public BufferHeader[] BufferHeaders;
        public BufferPart[][] BufferParts;
        public byte[][] BufferSkeletons;

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

            if (Header.UnknownInputOffset > 0) {
                reader.BaseStream.Position = Header.UnknownInputOffset;
                
                UnknownInputs = reader.ReadArray<UnknownInputDefinition>(Header.UnknownCount);
            }

            if (Header.VertexLayoutOffset > 0) {
                reader.BaseStream.Position = Header.VertexLayoutOffset;

                VertexLayout = reader.ReadArray<InputElementDefinitinon>(Header.VertexInputElementCount);
            }

            if (Header.BufferOffset > 0) {
                reader.BaseStream.Position = Header.BufferOffset;
                
                BufferHeaders = new BufferHeader[Header.BufferCount];
                BufferParts = new BufferPart[Header.BufferCount][];
                BufferSkeletons = new byte[Header.BufferCount][];
                for (int i = 0; i < Header.BufferCount; i++) {
                    long start = reader.BaseStream.Position;
                    var header = reader.Read<BufferHeader>();
                    BufferHeaders[i] = header;
                    long end = reader.BaseStream.Position;

                    if (header.SkeletonOffset != 0) {
                        reader.BaseStream.Position = start + header.SkeletonOffset;

                        BufferSkeletons[i] = reader.ReadBytes(header.BufferSize);
                    }
                    
                    reader.BaseStream.Position = start + header.PartOffset;
                    BufferParts[i] = reader.ReadArray<BufferPart>(header.PartCount);
                    
                    reader.BaseStream.Position = end;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BufferPart {
            public uint Hash;
            public ushort Size;
            public ushort Unk2Offset;
            public ushort Unk3Offset;
            public ushort Offset;
            
            // unsure:
            public byte ElementSize;
            public byte ElementCount;
            public byte StartIndex;
            public byte IsDynamic;  // todo: bool
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct BufferHeader {
            public long PartOffset;
            public long SkeletonOffset;
            public ulong Unknown2;
            public int BufferSize;
            public short PartCount;
            public short BufferIndex;
        }
    }
}