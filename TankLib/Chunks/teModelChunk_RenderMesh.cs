using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TankLib.Math;

namespace TankLib.Chunks {
    /// <inheritdoc />
    /// <summary>MRNM: Defines model render mesh</summary>
    public class teModelChunk_RenderMesh : IChunk {
        public string ID => "MRNM";
        
        /// <summary>MRNM header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct RenderMeshHeader {
            public uint VertexCount;
            public byte SubmeshCount;
            public byte UnknownCount1;
            public byte UnknownCount2;
            public byte UnknownCount3;
            public byte MaterialCount;
            public byte UnknownCount4;
            public byte VertexBufferDescriptorCount;
            public byte IndexBufferDescriptorCount;
            public fixed float unk5[13];
            public fixed uint unk6[4];
            public fixed float unk7[4];
            public fixed uint unk8[4];
            public long VertexBufferDesciptorPointer;
            public long IndexBufferDescriptorPointer;
            public long SubmeshDescriptorPointer;
            public long UnkADescriptorPointer;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct SubmeshDescriptor {
            public fixed float Unknown1[10];
            public long Unknown2Index;
            public float Unknown3;
            public uint VertexStart;
            public ushort IndexStart;
            public ushort Pad1;
            public fixed uint Unknown4[3];
            public ushort IndexCount;
            public ushort IndicesToDraw;
            public ushort VerticesToDraw;
            public ushort BoneIdOffset;
            public byte IndexBuffer;
            public fixed byte Pad2[7];
            public byte VertexBuffer;
            public SubmeshFlags Flags;
            public byte Material;
            public sbyte LOD;  // -1 = all
            public uint Unknown5;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct VertexBufferDescriptor {
            public uint VertexCount;
            public uint Unknown1;
            public byte StrideStream1;
            public byte StrideStream2;
            public byte VertexElementDescriptorCount;
            public byte Unknown2;
            public uint Unknown3;
            public long VertexElementDescriptorPointer;
            public long DataStream1Pointer;
            public long DataStream2Pointer;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct IndexBufferDescriptor {
            public uint IndexCount;
            public uint Format;
            public long DataStreamPointer;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct VertexElementDescriptor {
            public teShaderInstance.ShaderInputUse Type;
            public byte Index;
            public SemanticFormat Format;
            public byte Stream;
            public ushort Classification;
            public ushort Offset;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum SemanticFormat : byte {
            NONE = 0x0,
            SINGLE_3 = 0x2,
            HALF_2 = 0x4,
            UINT8_4 = 0x6,
            UINT8_UNORM4 = 0x8,
            UINT8_SNORM4 = 0x9,
            UINT32 = 0xC
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

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MeshFace {
            public ushort V1;
            public ushort V2;
            public ushort V3;

            public MeshFaceExport ToExportStruct() {
                return new MeshFaceExport {V1 = V1, V2 = V2, V3 = V3};
            }
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MeshFaceExport {
            public int V1;
            public int V2;
            public int V3;
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

            public Submesh(SubmeshDescriptor submeshDescriptor, byte uvCount) {
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
                    //BoneIndices[i] = new ushort[4];
                    BoneWeights[i] = new float[4];
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
            SubmeshDescriptors = reader.ReadArray<SubmeshDescriptor>(Header.SubmeshCount);
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
                long[] offset = {vbo.DataStream1Pointer, vbo.DataStream2Pointer};
                byte[] sizes = {vbo.StrideStream1, vbo.StrideStream2};
                VertexElementDescriptor[][] elements = SplitVBE(VertexElements[i]);
                for (int j = 0; j < offset.Length; ++j) {
                    Stride[i][j] = new object[vbo.VertexCount][];
                    reader.BaseStream.Position = offset[j];
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
            }
        }
        #endregion

        private object ReadElement(SemanticFormat format, BinaryReader reader) {
            switch (format) {
                case SemanticFormat.SINGLE_3:
                    return new[] {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()};
                case SemanticFormat.HALF_2:
                    return new[] {reader.ReadUInt16(), reader.ReadUInt16()};
                case SemanticFormat.UINT8_4:
                    return new[] {reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()};
                case SemanticFormat.UINT8_UNORM4:
                    return new[] {
                        reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f,
                        reader.ReadByte() / 255f
                    };
                case SemanticFormat.UINT8_SNORM4:
                    return new[] {
                        reader.ReadSByte() / 255f, reader.ReadSByte() / 255f, reader.ReadSByte() / 255f,
                        reader.ReadSByte() / 255f
                    };
                case SemanticFormat.NONE:
                    return null;
                case SemanticFormat.UINT32:
                    return reader.ReadUInt32();
                default:
                    if (Debugger.IsAttached)
                        Debugger.Log(2, "CHUNK_LDOMMNRM", $"Unhandled Semantic Format {format:X}!\n");
                    return null;
            }
        }

        /// <summary>Generate <see cref="Submesh"/> objects</summary>
        private void GenerateMeshes(BinaryReader reader) {
            Submeshes = new Submesh[SubmeshDescriptors.Length];
            
            for (int i = 0; i < SubmeshDescriptors.Length; ++i) {
                SubmeshDescriptor submeshDescriptor = SubmeshDescriptors[i];
                //VertexBufferDescriptor vbo = VertexBuffers[submeshDescriptor.VertexBuffer];
                IndexBufferDescriptor ibo = IndexBuffers[submeshDescriptor.IndexBuffer];
                byte uvCount = GetMaxIndex(VertexElements[submeshDescriptor.VertexBuffer], teShaderInstance.ShaderInputUse.TexCoord);
                
                Submesh submesh = new Submesh(submeshDescriptor, uvCount);
                
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
                                ushort[] uv = (ushort[]) value;
                                submesh.UV[k][element.Index] = teVec2.FromHalf(uv);
                            }
                                break;
                            case teShaderInstance.ShaderInputUse.BlendIndices:
                                if (element.Index == 0) {
                                    byte[] boneIndex = (byte[]) value;
                                    submesh.BoneIndices[k] = new ushort[boneIndex.Length];
                                    for (int m = 0; m < boneIndex.Length; ++m) {
                                        submesh.BoneIndices[k][m] = (ushort) (boneIndex[m] + submeshDescriptor.BoneIdOffset);
                                    }
                                } else {
                                    Debugger.Log(2, "teModelChunk_RenderMesh",
                                        $"Unhandled vertex layer {element.Index:X} for type {element.Type}!\n");
                                }
                                break;
                            case teShaderInstance.ShaderInputUse.BlendWeights:
                                if (element.Index == 0) {
                                    submesh.BoneWeights[k] = (float[]) value;
                                } else {
                                    Debugger.Log(2, "teModelChunk_RenderMesh",
                                        $"Unhandled vertex layer {element.Index:X} for type {element.Type}!\n");
                                }
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