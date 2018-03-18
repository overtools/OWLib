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
            public byte LOD;
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
            public SemanticType Type;
            public byte Index;
            public SemanticFormat Format;
            public byte Stream;
            public ushort Classification;
            public ushort Offset;
        }

        /// <summary>Vertex semantic type</summary>
        public enum SemanticType : byte {
            Position = 0x0,
            Normal = 0x1,
            Color = 0x2,
            Tangent = 0x3,
            BoneIndices = 0x4,
            BoneWeight = 0x5,
            Unknown1 = 0x6,
            Unknown2 = 0x7,
            Unknown3 = 0x8,
            UV = 0x9,
            ID = 0x10
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
        public enum SubmeshFlags : byte {
            Unk1 = 1,
            Unk2 = 2,
            Unk3 = 4,
            Unk4 = 8,
            /// <summary>Mesh with really low detail</summary>
            MinDetail = 16,
            Unk6 = 32,
            Unk7 = 64,
            Unk8 = 128
        }

        /// <summary>Parsed submesh</summary>
        public class Submesh {
            /// <summary>Vertex positions</summary>
            public teVec3[] Positions;
            
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
            
            /// <summary>Source descriptor</summary>
            public SubmeshDescriptor Descriptor;

            public Submesh(SubmeshDescriptor submeshDescriptor, byte uvCount) {
                Descriptor = submeshDescriptor;
                
                Positions = new teVec3[submeshDescriptor.VerticesToDraw];
                Normals = new teVec3[submeshDescriptor.VerticesToDraw];
                Tangents = new teVec4[submeshDescriptor.VerticesToDraw];
                IDs = new uint[submeshDescriptor.VerticesToDraw];
                
                Indices = new ushort[submeshDescriptor.IndicesToDraw];
                
                UV = new teVec2[submeshDescriptor.VerticesToDraw][];
                
                //UV = new teVec2[uvCount][];
                //for (int i = 0; i < uvCount; i++) {
                //    UV[i] = new teVec2[submeshDescriptor.VerticesToDraw];
                //}
                
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
        private static readonly HashSet<SemanticType> UnhandledSemanticTypes = new HashSet<SemanticType>();
        
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
            for (int i = 0; i < Header.VertexBufferDescriptorCount; ++i) {
                VertexBufferDescriptor descriptor = reader.Read<VertexBufferDescriptor>();
                VertexBuffers[i] = descriptor;
            }
            for (int i = 0; i < Header.VertexBufferDescriptorCount; ++i)
                VertexElements[i] = ParseVBE(reader, VertexBuffers[i]);
        }

        private void ParseIBO(BinaryReader reader) {
            reader.BaseStream.Position = Header.IndexBufferDescriptorPointer;
            for (int i = 0; i < Header.IndexBufferDescriptorCount; ++i) {
                IndexBufferDescriptor descriptor = reader.Read<IndexBufferDescriptor>();
                IndexBuffers[i] = descriptor;
            }
        }

        private void ParseSubmesh(BinaryReader reader) {
            reader.BaseStream.Position = Header.SubmeshDescriptorPointer;
            for (int i = 0; i < Header.SubmeshCount; ++i) {
                SubmeshDescriptor submesh = reader.Read<SubmeshDescriptor>();
                SubmeshDescriptors[i] = submesh;
            }
        }

        private VertexElementDescriptor[] ParseVBE(BinaryReader reader, VertexBufferDescriptor descriptor) {
            reader.BaseStream.Position = descriptor.VertexElementDescriptorPointer;
            VertexElementDescriptor[] elements = new VertexElementDescriptor[descriptor.VertexElementDescriptorCount];
            for (int i = 0; i < descriptor.VertexElementDescriptorCount; ++i)
                elements[i] = reader.Read<VertexElementDescriptor>();
            return elements;
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
                byte uvCount = GetMaxIndex(VertexElements[submeshDescriptor.VertexBuffer], SemanticType.UV);
                
                Submesh submesh = new Submesh(submeshDescriptor, uvCount);
                
                reader.BaseStream.Position = ibo.DataStreamPointer + submeshDescriptor.IndexStart * 2;
                
                Dictionary<int, ushort> indexRemap = new Dictionary<int, ushort>();
                Dictionary<int, int> indexRemapInvert = new Dictionary<int, int>();
                for (int j = 0; j < submeshDescriptor.IndicesToDraw; ++j) {
                    ushort indice = reader.ReadUInt16();
                    if (indexRemap.ContainsKey(indice)) {
                        indice = indexRemap[indice];
                    } else {
                        ushort indiceNew = (ushort) indexRemap.Count;
                        indexRemap[indice] = indiceNew;
                        indexRemapInvert[indiceNew] = indice;
                        indice = indiceNew;
                    }
                    submesh.Indices[j] = indice;
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
                            case SemanticType.Position:
                                if (element.Index == 0) {
                                    float[] position = (float[]) value;
                                    submesh.Positions[k] = new teVec3(position);
                                } else {
                                   Debugger.Log(2, "teModelChunk_RenderMesh",
                                       $"Unhandled vertex layer {element.Index:X} for type {element.Type}!\n");
                                }
                                break;
                            case SemanticType.Normal:
                                if (element.Index == 0) {
                                    float[] normal = (float[]) value;
                                    submesh.Normals[k] = new teVec3(normal.Take(3).ToArray());
                                } else {
                                    Debugger.Log(2, "teModelChunk_RenderMesh",
                                        $"Unhandled vertex layer {element.Index:X} for type {element.Type}!\n");
                                }
                                break;
                            case SemanticType.UV: {
                                ushort[] uv = (ushort[]) value;
                                submesh.UV[k][element.Index] = teVec2.FromHalf(uv);
                            }
                                break;
                            case SemanticType.BoneIndices:
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
                            case SemanticType.BoneWeight:
                                if (element.Index == 0) {
                                    submesh.BoneWeights[k] = (float[]) value;
                                } else {
                                    Debugger.Log(2, "teModelChunk_RenderMesh",
                                        $"Unhandled vertex layer {element.Index:X} for type {element.Type}!\n");
                                }
                                break;
                            case SemanticType.Tangent:
                                float[] tangent = (float[]) value;
                                submesh.Tangents[k] = new teVec4(tangent);
                                break;
                            case SemanticType.ID:
                                uint id = (uint) value;
                                submesh.IDs[k] = id;
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

        private byte GetMaxIndex(VertexElementDescriptor[] elements, SemanticType type) {
            byte max = 0;
            foreach (VertexElementDescriptor element in elements)
                if (element.Type == type) max = System.Math.Max(max, element.Index);
            max += 1;
            return max;
        }
    }
}