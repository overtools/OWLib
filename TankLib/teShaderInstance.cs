using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank ShaderInstance, file type 086</summary>
    public class teShaderInstance {
        /// <summary>ShaderInstance Header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct InstanceHeader {
            public long BufferOffset;  // 0
            
            /// <summary>Offset to texture input defintions</summary>
            public long ShaderResourceOffset;  // 8
            
            public long RWShaderResourceOffset; // 16
            
            /// <summary>Offset to buffer input defintions</summary>
            public long SamplerStatesOffset;  // 24
            
            /// <summary>Offset to vertex layut definition</summary>
            public long VertexLayoutOffset;  // 32
            
            /// <summary>teShaderCode reference</summary>
            /// <remarks>File type 087</remarks>
            public teResourceGUID ShaderCode;  // 40

            // these are just to get to the right position
            public long PadA;  // 48
            public uint PadB;  // 56
            
            public uint Unk1;  // 60
            
            /// <summary>CRC32b of the vertex input elements. Allows for easy layout reuse</summary>
            public uint ShaderInputCRC;  // 64
            
            //public short Unk3;
            public TestByteFlags Unk3;
            public byte Test;
            
            public short Unk4;
            
            public TestByteFlags Unk5;
            public byte Unk6;
            public byte Unk7;
            public byte Unk8;

            /// <summary>Constant buffer count</summary>
            public sbyte NumConstantBuffers;  // 76
            
            /// <summary>Shader resource definition count</summary>
            public sbyte NumShaderResources;  // 77
            public sbyte NumShaderRWResources;  // 78
            public sbyte SamplerStateCount;
            public sbyte VertexInputElementCount;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ShaderResourceDefinition {
            /// <summary>CRC32 Hash</summary>
            public uint NameHash;
            
            /// <summary>Shader resource index</summary>
            public byte Register;  // 5
            
            /// <summary>Type of shader resource this is</summary>
            public ShaderResourceType Type;  // 6
            public byte Format;  // 7
            public ViewDimension ViewDimension;
            public short Zero;
            
            /// <summary>Global resource index</summary>
            /// <note>This is calculated at runtime, CRC is looked up in an array in the client</note>
            public short GlobalIndex;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SamplerState {
            /// <summary>CRC32 Hash</summary>
            public uint NameHash;
            
            /// <summary>D3D register</summary>
            public short Register;
            
            /// <summary>Global sampler index</summary>
            /// <note>This is calculated at runtime, CRC is looked up in an array in the client</note>
            public short GlobalIndex;
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
            /// <summary>"Hash" of the data. Not sure if it is actually a hash or not</summary>
            public uint Hash;
            
            /// <summary>Size in bytes</summary>
            public ushort Size;
            public ushort Unk2Offset;
            public ushort Unk3Offset;
            
            /// <summary>Offset in bytes</summary>
            public ushort Offset;
            
            // unsure:
            public byte ElementSize;
            public byte ElementCount;
            
            /// <summary>If the "part" is global value or not?</summary>
            public byte IsDynamic; // todo: bool
            public byte Unknown;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct BufferHeader {
            /// <summary>Offset to "part" definitions</summary>
            public long PartOffset;
            
            /// <summary>Offset to skeleton byte array</summary>
            public long SkeletonOffset;
            
            /// <summary>Identifier hash</summary>
            public ulong Hash;
            
            /// <summary>Size in bytes</summary>
            public int BufferSize;
            
            /// <summary>Number of "parts" that make up this buffer</summary>
            /// <note>Most buffers now have no "parts" definied in the 086 data because blizzard keeps reading my code</note>
            public short PartCount;
            
            /// <summary>DX11 register</summary>
            public short Register;
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
        
        /// <summary>Read-write shader resources</summary>
        // ReSharper disable once InconsistentNaming
        public ShaderResourceDefinition[] RWShaderResources;
        
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
            
            if (Header.RWShaderResourceOffset != 0 && Header.NumShaderRWResources > -1) {
                reader.BaseStream.Position = Header.RWShaderResourceOffset;

                RWShaderResources = reader.ReadArray<ShaderResourceDefinition>(Header.NumShaderResources);
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