using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TACTLib;
using TankLib.Math;

namespace TankLib.Chunks {
    /// <inheritdoc />
    /// <summary>MRNM: Defines model render mesh</summary>
    public class teModelChunk_RenderMesh : IChunk {
        public string ID => "MRNM";
        public List<IChunk> SubChunks { get; set; }

        public static Func<teResourceGUID, Stream> LoadAssetFunc;

        /// <summary>MRNM header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct RenderMeshHeader {
            public long VertexBufferDesciptorPointer; // 112 -> 0
            public long IndexBufferDescriptorPointer; // 120 -> 8
            public long SubmeshDescriptorPointer; // 128 -> 16

            public uint VertexCount; // 0 -> 24
            public uint SubmeshCount; // 4 -> 28
            public ushort MaterialCount; // 8 -> 32
            public byte VertexBufferDescriptorCount; // 10 -> 34
            public byte IndexBufferDescriptorCount; // 11 -> 35
            public uint m_unk36; // 12 -> 36
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct SubmeshDescriptor {
            public fixed float Unknown1[3];// 0
            public fixed float Unknown1_A[4];// 12
            public fixed float Unknown1_B[3];// 28

            public long m_40; // 40 -> 40
            public long m_48; // n/a -> 48

            public float Unknown3; // 48 -> 56
            public uint VertexStart; // 52 -> 60

            public uint IndexStart; // 56 -> 64. ushort -> uint
            public fixed uint OtherIndexStarts[7]; // 60 -> 68 ushort -> uint

            public uint IndexCount; // 72 -> 96. word -> dword
            public uint IndicesToDraw; // 74 -> 100. word -> dword
            public ushort VerticesToDraw; // 76 -> 104
            public ushort BoneIdOffset; // 78 -> 106

            public byte IndexBuffer; // 80 -> 108
            public fixed byte OtherIndexBuffers[7];

            public byte VertexBuffer; // 88 -> 116
            public SubmeshFlags Flags; // 89 -> 117 probably
            public byte Material; // 90 -> 118
            public sbyte LOD;  // 91 -1 = all -> 119

            public uint m_120; // n/a -> 120
            public byte m_128; // n/a -> 128
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct VertexBufferDescriptor {
            public uint VertexCount; // 0
            public uint Unknown1; // 4
            public byte StrideStream1; // 8
            public byte StrideStream2; // 9
            public byte VertexElementDescriptorCount; // 10
            public byte Unknown2; // 11
            public uint Unknown3; // 12
            public long VertexElementDescriptorPointer; // 16
            public long DataStream1Pointer; // 24
            public long DataStream2Pointer; // 32

            public bool UsesVertexDataAsset() {
                return (Unknown2 & 0x10) != 0;
            }

            public teResourceGUID GetVertexDataGUID1() {
                if (!UsesVertexDataAsset()) throw new Exception("doesn't have vertex data");
                return (teResourceGUID)(ulong)DataStream1Pointer;
            }

            public teResourceGUID GetVertexDataGUID2() {
                if (!UsesVertexDataAsset()) throw new Exception("doesn't have vertex data");
                return (teResourceGUID)(ulong)DataStream2Pointer;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct IndexBufferDescriptor {
            public uint IndexCount;
            public uint Format;
            public long DataStreamPointer;
        }

        // todo: merge me with whatever is going on in shader
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct VertexElementDescriptor {
            public teShaderInstance.ShaderInputUse Type; // semantic type. rename me
            public byte Index; // semantic index. rename me
            public SemanticFormat Format;
            public byte Stream; // slot. rename me

            public uint m_val4;

            public byte SlotClass => (byte)(m_val4 & 0xF);
            public byte InstanceDataStepRate => (byte)(m_val4 >> 4);
            public byte Offset => (byte)((m_val4 >> 12) & 1023);

            // todo: >>22 = size?
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum SemanticFormat : byte {
            NONE = 0x0, // DXGI_FORMAT_R32_FLOAT??
            // DXGI_FORMAT_R32G32_FLOAT
            SINGLE_3 = 0x2, // DXGI_FORMAT_R32G32B32_FLOAT
            // DXGI_FORMAT_R32G32B32A32_FLOAT
            HALF_2 = 0x4, // DXGI_FORMAT_R16G16_FLOAT
            // DXGI_FORMAT_R16G16B16A16_FLOAT
            UINT8_4 = 0x6, // DXGI_FORMAT_R8G8B8A8_UINT
            UINT16_4 = 0x7, // DXGI_FORMAT_R16G16B16A16_UINT
            UINT8_UNORM4 = 0x8, // DXGI_FORMAT_R8G8B8A8_UNORM
            UINT8_SNORM4 = 0x9, // DXGI_FORMAT_R8G8B8A8_SNORM
            // DXGI_FORMAT_R16G16_SNORM
            // DXGI_FORMAT_R8_UINT
            UINT32 = 0xC // DXGI_FORMAT_R32_UINT
            // ...
        }

        [Flags]
        public enum SubmeshFlags : byte {  // todo: i'm not 100% sure about these
            Unk1 = 1,  // ?? nothing
            NonStatic = 2,  // moveable or animated
            Opaque = 4,
            Unk4 = 8,  // everything that isn't MinDetail?

            /// <summary>Mesh with really low detail</summary>
            MinDetail = 16,

            Unk6 = 32,  // same as NonStatic (-cloth physics?)
            Vegetation = 64,
            Unk8 = 128
        }

        /// <summary>Parsed submesh</summary>
        public class Submesh {
            /// <summary>Vertex positions</summary>
            public teVec3[] Vertices;

            /// <summary>Vertex normals</summary>
            public teVec3[] Normals;

            /// <summary>Vertex tangents</summary>
            public teVec4[] Tangents;

            /// <summary>Vertex UVs</summary>
            public teVec2[][] UV;

            /// <summary>Vertex IDs</summary>
            /// <remarks>Seems to be 0 most of the time</remarks>
            // ReSharper disable once InconsistentNaming
            public uint[] IDs;

            /// <summary>Vertex bone indices</summary>
            public ushort[][] BoneIndices;

            /// <summary>Vertex bone weights</summary>
            public float[][] BoneWeights;

            /// <summary>Triangle indices</summary>
            public ushort[] Indices;

            public teColorRGBA[] Color1;
            public teColorRGBA[] Color2;

            /// <summary>Source descriptor</summary>
            public SubmeshDescriptor Descriptor;

            /// <summary>Number of UV maps</summary>
            public byte UVCount;

            public Submesh(SubmeshDescriptor submeshDescriptor, byte uvCount, byte blendCount) {
                Descriptor = submeshDescriptor;
                UVCount = uvCount;

                Vertices = new teVec3[submeshDescriptor.VerticesToDraw];
                Normals = new teVec3[submeshDescriptor.VerticesToDraw];
                Tangents = new teVec4[submeshDescriptor.VerticesToDraw];
                IDs = new uint[submeshDescriptor.VerticesToDraw];

                Indices = new ushort[submeshDescriptor.IndicesToDraw];

                UV = new teVec2[submeshDescriptor.VerticesToDraw][];

                Color1 = new teColorRGBA[submeshDescriptor.VerticesToDraw];
                Color2 = new teColorRGBA[submeshDescriptor.VerticesToDraw];

                BoneIndices = new ushort[submeshDescriptor.VerticesToDraw][];
                BoneWeights = new float[submeshDescriptor.VerticesToDraw][];
                for (int i = 0; i < submeshDescriptor.VerticesToDraw; i++) {
                    BoneIndices[i] = new ushort[4*blendCount];
                    BoneWeights[i] = new float[4*blendCount];
                    UV[i] = new teVec2[uvCount];
                }
            }
        }

        /// <summary>Unhandled vertex semantic types</summary>
        private static readonly HashSet<teShaderInstance.ShaderInputUse> UnhandledSemanticTypes = new HashSet<teShaderInstance.ShaderInputUse>();

        /// <summary>Header data</summary>
        public RenderMeshHeader Header;

        /// <summary>Vertex buffer descriptors</summary>
        public VertexBufferDescriptor[] VertexBuffers;

        /// <summary>Index buffer descriptors</summary>
        public IndexBufferDescriptor[] IndexBuffers;

        /// <summary>Vertex elements</summary>
        public VertexElementDescriptor[][] VertexElements;

        /// <summary>
        ///     Vertex stride.
        ///     buffer -> stream -> vertex -> element
        /// </summary>
        public object[][][][] Stride; // buffer -> stream -> vertex -> element

        /// <summary>Submesh descriptions</summary>
        public SubmeshDescriptor[] SubmeshDescriptors;

        /// <summary>Parsed submeshes</summary>
        public Submesh[] Submeshes;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input)) {
                Header = reader.Read<RenderMeshHeader>();

                SubmeshDescriptors = new SubmeshDescriptor[Header.SubmeshCount];
                VertexBuffers = new VertexBufferDescriptor[Header.VertexBufferDescriptorCount];
                VertexElements = new VertexElementDescriptor[Header.VertexBufferDescriptorCount][];
                IndexBuffers = new IndexBufferDescriptor[Header.IndexBufferDescriptorCount];
                Stride = new object[Header.VertexBufferDescriptorCount][][][];

                ParseVBO(reader);
                ParseIBO(reader);
                ParseSubmesh(reader);
                ParseStride(reader);
                LoadVertexDataAssets(LoadAssetFunc);
                GenerateMeshes(reader);
            }
        }

        #region Parser Subfunctions
        private void ParseVBO(BinaryReader reader) {
            reader.BaseStream.Position = Header.VertexBufferDesciptorPointer;
            VertexBuffers = reader.ReadArray<VertexBufferDescriptor>(Header.VertexBufferDescriptorCount);
            for (int i = 0; i < Header.VertexBufferDescriptorCount; ++i)
                VertexElements[i] = ParseVBE(reader, VertexBuffers[i]);
        }

        private void ParseIBO(BinaryReader reader) {
            reader.BaseStream.Position = Header.IndexBufferDescriptorPointer;
            IndexBuffers = reader.ReadArray<IndexBufferDescriptor>(Header.IndexBufferDescriptorCount);
        }

        private void ParseSubmesh(BinaryReader reader) {
            reader.BaseStream.Position = Header.SubmeshDescriptorPointer;
            SubmeshDescriptors = reader.ReadArray<SubmeshDescriptor>((int)Header.SubmeshCount);
        }

        private VertexElementDescriptor[] ParseVBE(BinaryReader reader, VertexBufferDescriptor descriptor) {
            reader.BaseStream.Position = descriptor.VertexElementDescriptorPointer;
            return reader.ReadArray<VertexElementDescriptor>(descriptor.VertexElementDescriptorCount);
        }

        // split VBE by stream
        private VertexElementDescriptor[][] SplitVBE(VertexElementDescriptor[] input) {
            VertexElementDescriptor[][] elements = new VertexElementDescriptor[2][];

            // pass 1
            byte[] sizes = {0, 0};
            for (int i = 0; i < input.Length; ++i) sizes[input[i].Stream] += 1;

            // pass 2
            elements[0] = new VertexElementDescriptor[sizes[0]];
            elements[1] = new VertexElementDescriptor[sizes[1]];
            sizes = new byte[] {0, 0};
            for (int i = 0; i < input.Length; ++i) {
                byte stream = input[i].Stream;
                elements[stream][sizes[stream]] = input[i];
                sizes[stream] += 1;
            }
            return elements;
        }

        // buffer -> stream -> vertex -> element
        private void ParseStride(BinaryReader reader) {
            for (int i = 0; i < VertexBuffers.Length; ++i) {
                VertexBufferDescriptor vbo = VertexBuffers[i];
                Stride[i] = new object[2][][];

                if (vbo.UsesVertexDataAsset()) continue;

                long[] offset = {vbo.DataStream1Pointer, vbo.DataStream2Pointer};
                byte[] sizes = {vbo.StrideStream1, vbo.StrideStream2};
                VertexElementDescriptor[][] elements = SplitVBE(VertexElements[i]);
                for (int j = 0; j < offset.Length; ++j) {
                    ParseVBStrideHalf(reader, i, j, vbo, offset[j], elements, sizes);
                }
            }
        }

        public void LoadVertexDataAssets(Func<teResourceGUID, Stream> loadFunc) {
            if (loadFunc == null) return;
            for (int i = 0; i < VertexBuffers.Length; ++i) {
                VertexBufferDescriptor vbo = VertexBuffers[i];
                if (!vbo.UsesVertexDataAsset()) continue;

                byte[] sizes = {vbo.StrideStream1, vbo.StrideStream2};
                VertexElementDescriptor[][] elements = SplitVBE(VertexElements[i]);

                for (int j = 0; j < 2; j++) {
                    teResourceGUID vertexDataAsset;
                    if (j == 0) {
                        vertexDataAsset = vbo.GetVertexDataGUID1();
                    } else {
                        vertexDataAsset = vbo.GetVertexDataGUID2();
                    }

                    using (var vertexReader = new BinaryReader(loadFunc(vertexDataAsset))) {
                        ParseVBStrideHalf(vertexReader, i, j, vbo, 0, elements, sizes);
                    }
                }
            }
        }

        private void ParseVBStrideHalf(BinaryReader reader, int i, int j, VertexBufferDescriptor vbo, long offset, VertexElementDescriptor[][] elements, byte[] sizes) {
            Stride[i][j] = new object[vbo.VertexCount][];
            reader.BaseStream.Position = offset;
            for (int k = 0; k < vbo.VertexCount; ++k) {
                Stride[i][j][k] = new object[elements[j].Length];
                long next = reader.BaseStream.Position + sizes[j];
                long current = reader.BaseStream.Position;
                for (int l = 0; l < elements[j].Length; ++l) {
                    reader.BaseStream.Position = current + elements[j][l].Offset;
                    Stride[i][j][k][l] = ReadElement(elements[j][l].Format, reader);
                }
                reader.BaseStream.Position = next;
            }
        }

        #endregion

        private object ReadElement(SemanticFormat format, BinaryReader reader) {
            switch (format) {
                case SemanticFormat.SINGLE_3:
                    return new[] {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()};
                case SemanticFormat.HALF_2:
                    return new[] {reader.ReadHalf(), reader.ReadHalf()};
                case SemanticFormat.UINT8_4:
                    return new[] {reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()};
                case SemanticFormat.UINT16_4:
                    return new[] {reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16()};
                case SemanticFormat.UINT8_UNORM4:
                    return new[] {
                        reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f
                    };
                case SemanticFormat.UINT8_SNORM4:
                    return new[] {
                        reader.ReadSByte() / 128f, reader.ReadSByte() / 128f, reader.ReadSByte() / 128f, reader.ReadSByte() / 128f
                    };
                case SemanticFormat.UINT32:
                    return reader.ReadUInt32();
                default:
                    throw new NotImplementedException($"Unhandled Semantic Format {format:X}!\n");
            }
        }

        /// <summary>Generate <see cref="Submesh"/> objects</summary>
        private void GenerateMeshes(BinaryReader reader) {
            Submeshes = new Submesh[SubmeshDescriptors.Length];

            for (int i = 0; i < SubmeshDescriptors.Length; ++i) {
                SubmeshDescriptor submeshDescriptor = SubmeshDescriptors[i];
                VertexBufferDescriptor vbo = VertexBuffers[submeshDescriptor.VertexBuffer];
                if (vbo.UsesVertexDataAsset() && Stride[submeshDescriptor.VertexBuffer][0] == null) {
                    Logger.Warn("teModelChunk_RenderMesh", $"Can't generate mesh {i} because vertex data assets were not loaded! Set LoadAssetFunc");
                    continue;
                }

                IndexBufferDescriptor ibo = IndexBuffers[submeshDescriptor.IndexBuffer];
                byte uvCount = GetMaxIndex(VertexElements[submeshDescriptor.VertexBuffer], teShaderInstance.ShaderInputUse.TexCoord);
                byte blendCount = GetMaxIndex(VertexElements[submeshDescriptor.VertexBuffer], teShaderInstance.ShaderInputUse.BlendWeights);

                Submesh submesh = new Submesh(submeshDescriptor, uvCount, blendCount);

                //Console.Out.WriteLine($"SUBMESH: ptr {ibo.DataStreamPointer} indexstart: {submeshDescriptor.IndexStart} {submeshDescriptor.VertexStart} {submeshDescriptor.VerticesToDraw}");
                reader.BaseStream.Position = ibo.DataStreamPointer + submeshDescriptor.IndexStart * 2;
                Dictionary<int, ushort> indexRemap = new Dictionary<int, ushort>();
                Dictionary<int, int> indexRemapInvert = new Dictionary<int, int>();

                // todo: make this cleaner
                for (int j = 0; j < submeshDescriptor.IndicesToDraw; j++) {
                    ushort index = reader.ReadUInt16();
                    ushort newIndex;
                    if (indexRemap.TryGetValue(index, out ushort remappedIndex)) {
                        newIndex = remappedIndex;  // "index of", value = fake index
                    } else {
                        newIndex = (ushort) indexRemap.Count;
                        indexRemap[index] = newIndex;
                        indexRemapInvert[newIndex] = index;
                    }

                    submesh.Indices[j] = newIndex;
                }

                VertexElementDescriptor[][] elements = SplitVBE(VertexElements[submeshDescriptor.VertexBuffer]);
                for (int j = 0; j < Stride[submeshDescriptor.VertexBuffer].Length; ++j)
                for (int k = 0; k < submeshDescriptor.VerticesToDraw; ++k) {
                    long offset = submeshDescriptor.VertexStart + indexRemapInvert[k];
                    for (int l = 0; l < elements[j].Length; ++l) {
                        VertexElementDescriptor element = elements[j][l];
                        if (element.Format == SemanticFormat.NONE) break;
                        object value = Stride[submeshDescriptor.VertexBuffer][j][offset][l];
                        switch (element.Type) {
                            case teShaderInstance.ShaderInputUse.Position:
                                if (element.Index == 0) {
                                    float[] position = (float[]) value;
                                    submesh.Vertices[k] = new teVec3(position);
                                } else {
                                   Debugger.Log(2, "teModelChunk_RenderMesh",
                                       $"Unhandled vertex layer {element.Index:X} for type {element.Type}!\n");
                                }
                                break;
                            case teShaderInstance.ShaderInputUse.Normal:
                                if (element.Index == 0) {
                                    float[] normal = (float[]) value;
                                    submesh.Normals[k] = new teVec3(normal.Take(3).ToArray());
                                } else {
                                    Debugger.Log(2, "teModelChunk_RenderMesh",
                                        $"Unhandled vertex layer {element.Index:X} for type {element.Type}!\n");
                                }
                                break;
                            case teShaderInstance.ShaderInputUse.TexCoord: {
                                Half[] uv = (Half[]) value;
                                submesh.UV[k][element.Index] = teVec2.FromHalf(uv);
                            }
                                break;
                            case teShaderInstance.ShaderInputUse.BlendIndices:
                                ushort[] thisIndices = (ushort[]) value;
                                var indicesDest = submesh.BoneIndices[k].AsSpan(element.Index * 4, 4);

                                for (int m = 0; m < thisIndices.Length; ++m) {
                                    indicesDest[m] = (ushort) (thisIndices[m] + submeshDescriptor.BoneIdOffset);
                                }
                                break;
                            case teShaderInstance.ShaderInputUse.BlendWeights:
                                var thisWeights = (float[]) value;
                                var weightsDest = submesh.BoneWeights[k].AsSpan(element.Index * 4, 4);
                                thisWeights.AsSpan().CopyTo(weightsDest);
                                break;
                            case teShaderInstance.ShaderInputUse.Tangent:
                                float[] tangent = (float[]) value;
                                submesh.Tangents[k] = new teVec4(tangent);
                                break;
                            case teShaderInstance.ShaderInputUse.VertexIndex:  // todo: lolno
                                uint id = (uint) value;
                                submesh.IDs[k] = id;
                                break;
                            case teShaderInstance.ShaderInputUse.Color:
                                float[] col = (float[]) value;
                                if (element.Index == 0) {
                                    submesh.Color1[k] = new teColorRGBA(col);
                                } else if (element.Index == 1) {
                                    submesh.Color2[k] = new teColorRGBA(col);
                                } else {
                                    Debugger.Log(2, "teModelChunk_RenderMesh",
                                        $"Unhandled vertex color index: {element.Index:X}\n");
                                }

                                break;
                            default:
                                if (UnhandledSemanticTypes.Add(element.Type) && Debugger.IsAttached) {
                                    Debugger.Log(2, "teModelChunk_RenderMesh",
                                        $"Unhandled vertex type {element.Type}!\n");
                                }
                                break;
                        }
                    }
                }

                Submeshes[i] = submesh;
            }
        }

        private byte GetMaxIndex(VertexElementDescriptor[] elements, teShaderInstance.ShaderInputUse type) {
            byte max = 0;
            foreach (VertexElementDescriptor element in elements)
                if (element.Type == type) max = System.Math.Max(max, element.Index);
            max += 1;
            return max;
        }
    }
}