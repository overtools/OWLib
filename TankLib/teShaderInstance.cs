using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank ShaderInstance, file type 086</summary>
    public class teShaderInstance {
        /// <summary>ShaderInstance Header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct InstanceHeader {
            public long BufferOffset;  // 0 -> 0
            public long ShaderResourceOffset;  // 8 -> 8
            //public long RWShaderResourceOffset; // 16 -> n/a
            public long SamplerStatesOffset;  // 24 -> 16
            public long VertexLayoutOffset;  // 32 -> 24  

            public long m_new32; // n/a -> 32
            public long m_new40; // n/a -> 40
            
            public teResourceGUID ShaderCode;  // 40 -> 48

            // these are just to get to the right position
            public long PadA;  // 48 -> 56
            public uint PadB;  // 56 -> 64
            
            public uint m_pad68;
            public uint m_pad72;
            
            public uint Unk1;  // 60 -> 76
            public uint ShaderInputCRC;  // 64 -> 80
            
            // 84... who knows
            public ulong m_pad84;
            public short m_pad92;
            
            public short Unk4; // 70 -> 94
            
            public uint m_pad96;
            public short m_pad100;

            /// <summary>Constant buffer count</summary>
            public byte NumConstantBuffers;  // 76 -> 102
            public byte NumShaderResources;  // 77 -> 103
            //public byte NumShaderRWResources;  // 78 -> n/a
            public byte SamplerStateCount; // 79 -> 104
            public byte VertexInputElementCount; // 80 -> 105
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ShaderResourceDefinition {
            public uint NameHash; // 0
            public byte Register;  // 4
            public byte m_flags; // 5
            
            public ViewDimension ViewDimension; // 7 -> 6
            public byte m_7; // n/a -> 7
            
            public byte m_8; // 8
            public byte m_9; // 8
            public byte m_10; // 10
            
            public byte GlobalIndex; // 10 -> 11. short -> byte
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SamplerState {
            public uint NameHash; // 0
            public byte Register; // 4
            
            public byte m_5; // n/a -> 5
            public byte m_6; // n/a -> 6
            public byte m_7; // n/a -> 7
            
            public sbyte GlobalIndex; // 6 -> 8. word -> sbyte
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct InputElementDefinition {
            public ShaderInputUse InputUse;
            public byte Register;
            public ShaderInputForm Format;

            public byte Slot;
            public byte Unk1;
            public byte Unk2;
            public byte Unk3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BufferPart {
            public uint Hash; // 0
            public ushort Size; // 4
            public ushort Offset; // 10 (long ago) -> 6
            
            // unsure:
            public byte ElementSize;
            public byte ElementCount;
            public byte IsDynamic;
            public byte Unknown;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct BufferHeader {
            public long PartOffset; // 0
            public long SkeletonOffset; // 8 -> 8
            public ushort BufferSize; // 24 -> 16. dword -> word
            
            public uint m_pad18;
            public ushort m_pad22;
            
            public ulong Hash; // 16 -> 24
            public short PartCount; // 28 -> 32
            public byte Register; // 30 -> 34
        }

        public enum ShaderResourceType : byte {
            Buffer = 0,
            Texture = 1
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum ShaderInputUse : byte {
            Position = 0,
            Normal = 1,
            Binormal = 2,
            Tangent = 3,
            BlendIndices = 4,
            BlendWeights = 5,
            PositionT = 6,
            PSize = 7,
            Color = 8,
            TexCoord = 9,
            VFace = 10,
            VPos = 11,
            MtxInstWS = 12,
            SV_Position = 13,
            RegionMask = 14,
            InstanceData = 15,
            VertexIndex = 16
        }

        public enum ShaderInputForm : short {
            Float2 = 1,
            Float3 = 2,
            Float4 = 3,
            ColorRGBA = 8,
            UInt = 12,
            UInt4 = 13,
            Int4 = 16,
            UInt2 = 17
        }

        public enum ViewDimension : byte {
            Buffer = 0, // ?
            
            SRVTexture1D = 1,
            SRVTexture2D = 2,
            SRVTexture3D = 3,
            SRVTextureCube = 4,
            SRVTexture1DArray = 5,
            SRVTexture2DArray = 6,
            SRVTextureCubeArray = 7,
            
            UAVTexture1D = 8,
            UAVTexture2D = 9,
            UAVTexture3D = 10,
            UAVTexture1DArray = 11,
            UAVTexture2DArray = 12
        }

        /// <summary>Header data</summary>
        public InstanceHeader Header;
        
        /// <summary>Shader resources</summary>
        public ShaderResourceDefinition[] ShaderResources;
        
        /// <summary>Shader samplers</summary>
        public SamplerState[] SamplerStates;

        /// <summary>Shader vertex layout</summary>
        public InputElementDefinition[] VertexLayout;
        
        /// <summary>Shader constant buffer definitions</summary>
        public BufferHeader[] BufferHeaders;
        
        /// <summary>"Parts" that are combined together to create a shader constant buffer</summary>
        public BufferPart[][] BufferParts;
        
        /// <summary> The data skeleton that buffers are built on. null = no skeleton</summary>
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
            
            if (Header.ShaderResourceOffset != 0 && Header.NumShaderResources > -1) {
                reader.BaseStream.Position = Header.ShaderResourceOffset;

                ShaderResources = reader.ReadArray<ShaderResourceDefinition>(Header.NumShaderResources);
            }

            if (Header.SamplerStatesOffset > 0) {
                reader.BaseStream.Position = Header.SamplerStatesOffset;
                
                SamplerStates = reader.ReadArray<SamplerState>(Header.SamplerStateCount);
            }

            if (Header.VertexLayoutOffset > 0) {
                reader.BaseStream.Position = Header.VertexLayoutOffset;

                VertexLayout = reader.ReadArray<InputElementDefinition>(Header.VertexInputElementCount);
            }
            
            if (Header.BufferOffset > 0) {
                reader.BaseStream.Position = Header.BufferOffset;
                
                BufferHeaders = new BufferHeader[Header.NumConstantBuffers];
                BufferParts = new BufferPart[Header.NumConstantBuffers][];
                BufferSkeletons = new byte[Header.NumConstantBuffers][];
                for (int i = 0; i < Header.NumConstantBuffers; i++) {
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
    }
}