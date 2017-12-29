using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

// ReSharper disable once CheckNamespace
namespace OWLib.Types.Chunk {
    public class MNRM : IChunk {
        public string Identifier => "MNRM"; // MRNM - Model ? ? ?
        public string RootIdentifier => "LDOM"; // MODL - Model

        private static readonly HashSet<SemanticFormat> UnhandledSemanticFormats = new HashSet<SemanticFormat>();
        private static readonly HashSet<SemanticType> UnhandledSemanticTypes = new HashSet<SemanticType>();

        public MeshDescriptor Mesh;
        public VertexBufferDescriptor[] VertexBuffers;
        public IndexBufferDescriptor[] IndexBuffers;
        public VertexElementDescriptor[][] VertexElements;
        public SubmeshDescriptor[] Submeshes;

        public ModelVertex[][] Vertices;
        public ModelVertex[][] Normals;
        public ModelIndice[][] Indices;
        public ModelUV[][][] TextureCoordinates;
        public ModelBoneData[][] Bones;
        public object[][][][] Stride; // buffer -> stream -> vertex -> element

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
                Mesh = reader.Read<MeshDescriptor>();

                VertexBuffers = new VertexBufferDescriptor[Mesh.vertexBufferDescriptorCount];
                VertexElements = new VertexElementDescriptor[Mesh.vertexBufferDescriptorCount][];
                IndexBuffers = new IndexBufferDescriptor[Mesh.indexBufferDescriptorCount];
                Stride = new object[Mesh.vertexBufferDescriptorCount][][][];

                Submeshes = new SubmeshDescriptor[Mesh.submeshDescriptorCount];
                Vertices = new ModelVertex[Mesh.submeshDescriptorCount][];
                Normals = new ModelVertex[Mesh.submeshDescriptorCount][];
                Indices = new ModelIndice[Mesh.submeshDescriptorCount][];
                TextureCoordinates = new ModelUV[Mesh.submeshDescriptorCount][][];
                Bones = new ModelBoneData[Mesh.submeshDescriptorCount][];

                ParseVBO(reader);
                ParseIBO(reader);
                ParseSubmesh(reader);
                ParseStride(reader);
                GenerateMeshes(reader);
            }
        }

        #region Parser Subfunctions

        public void ParseVBO(BinaryReader reader) {
            reader.BaseStream.Position = Mesh.vertexBufferDesciptorPointer;
            for (int i = 0; i < Mesh.vertexBufferDescriptorCount; ++i) {
                VertexBufferDescriptor descriptor = reader.Read<VertexBufferDescriptor>();
                VertexBuffers[i] = descriptor;
            }
            for (int i = 0; i < Mesh.vertexBufferDescriptorCount; ++i)
                VertexElements[i] = ParseVBE(reader, VertexBuffers[i]);
        }

        public void ParseIBO(BinaryReader reader) {
            reader.BaseStream.Position = Mesh.indexBufferDescriptorPointer;
            for (int i = 0; i < Mesh.indexBufferDescriptorCount; ++i) {
                IndexBufferDescriptor descriptor = reader.Read<IndexBufferDescriptor>();
                IndexBuffers[i] = descriptor;
            }
        }

        public void ParseSubmesh(BinaryReader reader) {
            reader.BaseStream.Position = Mesh.submeshDescriptorPointer;
            for (int i = 0; i < Mesh.submeshDescriptorCount; ++i) {
                SubmeshDescriptor submesh = reader.Read<SubmeshDescriptor>();
                Submeshes[i] = submesh;
            }
        }

        public VertexElementDescriptor[] ParseVBE(BinaryReader reader, VertexBufferDescriptor descriptor) {
            reader.BaseStream.Position = descriptor.vertexElementDescriptorPointer;
            VertexElementDescriptor[] elements = new VertexElementDescriptor[descriptor.vertexElementDescriptorCount];
            for (int i = 0; i < descriptor.vertexElementDescriptorCount; ++i)
                elements[i] = reader.Read<VertexElementDescriptor>();
            return elements;
        }

        // split VBE by stream
        public VertexElementDescriptor[][] SplitVBE(VertexElementDescriptor[] input) {
            VertexElementDescriptor[][] elements = new VertexElementDescriptor[2][];

            // pass 1
            byte[] sizes = new byte[2] {0, 0};
            for (int i = 0; i < input.Length; ++i) sizes[input[i].stream] += 1;

            // pass 2
            elements[0] = new VertexElementDescriptor[sizes[0]];
            elements[1] = new VertexElementDescriptor[sizes[1]];
            sizes = new byte[2] {0, 0};
            for (int i = 0; i < input.Length; ++i) {
                byte stream = input[i].stream;
                elements[stream][sizes[stream]] = input[i];
                sizes[stream] += 1;
            }
            return elements;
        }

        // buffer -> stream -> vertex -> element
        public void ParseStride(BinaryReader reader) {
            for (int i = 0; i < VertexBuffers.Length; ++i) {
                VertexBufferDescriptor vbo = VertexBuffers[i];
                Stride[i] = new object[2][][];
                long[] offset = new long[2] {vbo.dataStream1Pointer, vbo.dataStream2Pointer};
                byte[] sizes = new byte[2] {vbo.strideStream1, vbo.strideStream2};
                VertexElementDescriptor[][] elements = SplitVBE(VertexElements[i]);
                for (int j = 0; j < offset.Length; ++j) {
                    Stride[i][j] = new object[vbo.vertexCount][];
                    reader.BaseStream.Position = offset[j];
                    for (int k = 0; k < vbo.vertexCount; ++k) {
                        Stride[i][j][k] = new object[elements[j].Length];
                        long next = reader.BaseStream.Position + sizes[j];
                        long current = reader.BaseStream.Position;
                        for (int l = 0; l < elements[j].Length; ++l) {
                            reader.BaseStream.Position = current + elements[j][l].offset;
                            Stride[i][j][k][l] = ReadElement(elements[j][l].format, reader);
                        }
                        reader.BaseStream.Position = next;
                    }
                }
            }
        }

        #endregion

        public object ReadElement(SemanticFormat format, BinaryReader reader) {
            switch (format) {
                case SemanticFormat.SINGLE_3:
                    return new float[3] {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()};
                case SemanticFormat.HALF_2:
                    return new ushort[2] {reader.ReadUInt16(), reader.ReadUInt16()};
                case SemanticFormat.UINT8_4:
                    return new byte[4] {reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()};
                case SemanticFormat.UINT8_UNORM4:
                    return new float[4] {
                        reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f,
                        reader.ReadByte() / 255f
                    };
                case SemanticFormat.UINT8_SNORM4:
                    return new float[4] {
                        reader.ReadSByte() / 255f, reader.ReadSByte() / 255f, reader.ReadSByte() / 255f,
                        reader.ReadSByte() / 255f
                    };
                case SemanticFormat.NONE:
                    return null;
                case SemanticFormat.UINT32:
                    return reader.ReadUInt32();
                default:
                    if (UnhandledSemanticFormats.Add(format))
                        if (Debugger.IsAttached)
                            Debugger.Log(2, "CHUNK_LDOMMNRM", $"Unhandled Semantic Format {format:X}!\n");
                    return null;
            }
        }

        private byte GetMaxIndex(VertexElementDescriptor[] elements, SemanticType type) {
            byte max = 0;
            foreach (VertexElementDescriptor element in elements)
                if (element.type == type) max = Math.Max(max, element.index);
            max += 1;
            return max;
        }

        public void GenerateMeshes(BinaryReader reader) {
            // TODO
            for (int i = 0; i < Submeshes.Length; ++i) {
                SubmeshDescriptor submesh = Submeshes[i];
                VertexBufferDescriptor vbo = VertexBuffers[submesh.vertexBuffer];
                IndexBufferDescriptor ibo = IndexBuffers[submesh.indexBuffer];
                byte uvCount = GetMaxIndex(VertexElements[submesh.vertexBuffer], SemanticType.UV);

                #region IBO

                ModelIndice[] indices = new ModelIndice[submesh.indicesToDraw / 3];

                reader.BaseStream.Position = ibo.dataStreamPointer + submesh.indexStart * 2;
                Dictionary<int, ushort> indexTracker = new Dictionary<int, ushort>();
                Dictionary<int, int> indexTrackerInvert = new Dictionary<int, int>();
                for (int j = 0; j < submesh.indicesToDraw / 3; ++j) {
                    ModelIndice index = reader.Read<ModelIndice>();
                    ushort v1;
                    ushort v2;
                    ushort v3;
                    if (indexTracker.ContainsKey(index.v1)) {
                        v1 = indexTracker[index.v1];  // "index of", value = fake index
                    } else {
                        v1 = (ushort) indexTracker.Count;
                        indexTracker[index.v1] = v1;
                        indexTrackerInvert[v1] = index.v1;
                    }
                    if (indexTracker.ContainsKey(index.v2)) {
                        v2 = indexTracker[index.v2];
                    } else {
                        v2 = (ushort) indexTracker.Count;
                        indexTracker[index.v2] = v2;
                        indexTrackerInvert[v2] = index.v2;
                    }
                    if (indexTracker.ContainsKey(index.v3)) {
                        v3 = indexTracker[index.v3];
                    } else {
                        v3 = (ushort) indexTracker.Count;
                        indexTracker[index.v3] = v3;
                        indexTrackerInvert[v3] = index.v3;
                    }

                    indices[j] = new ModelIndice {v1 = v1, v2 = v2, v3 = v3};
                }
                Indices[i] = indices;

                #endregion

                #region UV Pre.

                ModelUV[][] uv = new ModelUV[uvCount][];
                for (int j = 0; j < uvCount; ++j) uv[j] = new ModelUV[submesh.verticesToDraw];

                #endregion

                #region VBO

                ModelVertex[] vertex = new ModelVertex[submesh.verticesToDraw];
                ModelBoneData[] bone = new ModelBoneData[submesh.verticesToDraw];
                ModelVertex[] normal = new ModelVertex[submesh.verticesToDraw];

                VertexElementDescriptor[][] elements = SplitVBE(VertexElements[submesh.vertexBuffer]);
                for (int j = 0; j < Stride[submesh.vertexBuffer].Length; ++j)
                for (int k = 0; k < submesh.verticesToDraw; ++k) {
                    long offset = submesh.vertexStart + indexTrackerInvert[k];
                    for (int l = 0; l < elements[j].Length; ++l) {
                        VertexElementDescriptor element = elements[j][l];
                        if (element.format == SemanticFormat.NONE) break;
                        object value = Stride[submesh.vertexBuffer][j][offset][l];
                        switch (element.type) {
                            case SemanticType.POSITION:
                                if (element.index == 0) {
                                    float[] t = (float[]) value;
                                    vertex[k] = new ModelVertex {x = t[0], y = t[1], z = t[2]};
                                } else {
                                    if (UnhandledSemanticTypes.Add(element.type) && Debugger.IsAttached)
                                        Debugger.Log(2, "CHUNK_LDOMMNRM",
                                            $"Unhandled vertex layer {element.index:X} for type {Util.GetEnumName(typeof(SemanticType), element.type)}!\n");
                                }
                                break;
                            case SemanticType.NORMAL:
                                if (element.index == 0) {
                                    float[] t = (float[]) value;
                                    normal[k] = new ModelVertex {x = t[0], y = t[1], z = t[2]};
                                } else {
                                    if (UnhandledSemanticTypes.Add(element.type) && Debugger.IsAttached)
                                        Debugger.Log(2, "CHUNK_LDOMMNRM",
                                            $"Unhandled vertex layer {element.index:X} for type {Util.GetEnumName(typeof(SemanticType), element.type)}!\n");
                                }
                                break;
                            case SemanticType.UV: {
                                ushort[] t = (ushort[]) value;
                                uv[element.index][k] = new ModelUV {u = Half.ToHalf(t[0]), v = Half.ToHalf(t[1])};
                            }
                                break;
                            case SemanticType.BONE_INDEX:
                                if (element.index == 0) {
                                    byte[] t = (byte[]) value;
                                    bone[k].boneIndex = new ushort[t.Length];
                                    for (int m = 0; m < t.Length; ++m)
                                        bone[k].boneIndex[m] = (ushort) (t[m] + submesh.boneIdOffset);
                                } else {
                                    if (UnhandledSemanticTypes.Add(element.type) && Debugger.IsAttached)
                                        Debugger.Log(2, "CHUNK_LDOMMNRM",
                                            $"Unhandled vertex layer {element.index:X} for type {Util.GetEnumName(typeof(SemanticType), element.type)}!\n");
                                }
                                break;
                            case SemanticType.BONE_WEIGHT:
                                if (element.index == 0) {
                                    bone[k].boneWeight = (float[]) value;
                                } else {
                                    if (UnhandledSemanticTypes.Add(element.type) && Debugger.IsAttached)
                                        Debugger.Log(2, "CHUNK_LDOMMNRM",
                                            $"Unhandled vertex layer {element.index:X} for type {Util.GetEnumName(typeof(SemanticType), element.type)}!\n");
                                }
                                break;
                            default:
                                if (UnhandledSemanticTypes.Add(element.type) && Debugger.IsAttached)
                                    Debugger.Log(2, "CHUNK_LDOMMNRM",
                                        $"Unhandled vertex type {Util.GetEnumName(typeof(SemanticType), element.type)}!\n");
                                break;
                        }
                    }
                }

                indexTracker.Clear();
                indexTrackerInvert.Clear();

                TextureCoordinates[i] = uv;
                Vertices[i] = vertex;
                Bones[i] = bone;
                Normals[i] = normal;

                #endregion
            }
        }
    }
}