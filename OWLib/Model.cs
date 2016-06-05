using System;
using System.Collections.Generic;
using System.IO;
using OWLib.Types;

namespace OWLib {
  public class Model {
    private byte vertexBufferCount;
    private byte indiceBufferCount;
    private byte submeshCount;
    private ushort boneCount;
    
    private HashSet<ModelVertexType> unhandledVertexTypes;
    private HashSet<ModelVertexFormat> unhandledVertexFormats;
    private short[] boneHierarchy;
    private ushort[] boneLookup;
    private float[][] boneData;
    private ModelSubmesh[] submeshes;
    private ModelVertexBuffer[] vertexBuffers;
    private ModelIndiceBuffer[] indiceBuffers;
    private ModelUV[][] uvs;
    private ModelIndice[][] faces;
    private ModelVertex[][] vertices;
    private ModelVertex[][] normals;
    private ModelBoneData[][] bones;
    private ModelVertexElement[][][] vertexElements;
    private object[][][][] vertexStrideStream;

    public short[] BoneHierarchy => boneHierarchy;
    public ushort[] BoneLookup => boneLookup;
    public float[][] BoneData => boneData;
    public ModelSubmesh[] Submeshes => submeshes;
    public ModelVertexBuffer[] VertexBuffers => vertexBuffers;
    public ModelIndiceBuffer[] IndiceBuffers => indiceBuffers;
    public ModelUV[][] UVs => uvs;
    public ModelIndice[][] Faces => faces;
    public ModelVertex[][] Vertices => vertices;
    public ModelVertex[][] Normals => normals;
    public ModelBoneData[][] Bones => bones;
    public ModelVertexElement[][][] VertexElements => vertexElements;
    public object[][][] VertexStrideStream => vertexStrideStream;

    public object ReadStrideValue(ModelVertexFormat format, BinaryReader modelReader) {
      switch(format) {
        case ModelVertexFormat.SINGLE_3:
          return new float[3] { modelReader.ReadSingle(), modelReader.ReadSingle(), modelReader.ReadSingle() };
        case ModelVertexFormat.HALF_2:
          return new Half[2] { Half.ToHalf(modelReader.ReadUInt16()), Half.ToHalf(modelReader.ReadUInt16()) };
        case ModelVertexFormat.UINT8_4:
          return new byte[4] { modelReader.ReadByte(), modelReader.ReadByte(), modelReader.ReadByte(), modelReader.ReadByte() };
        case ModelVertexFormat.UINT8_UNORM4:
          return new float[4] { (float)(modelReader.ReadByte()) / 255, (float)(modelReader.ReadByte())  / 255, (float)(modelReader.ReadByte())  / 255, (float)(modelReader.ReadByte())  / 255 };
        case ModelVertexFormat.UINT8_SNORM4:
          return new float[4] { (float)(modelReader.ReadSByte()) / 255, (float)(modelReader.ReadSByte()) / 255, (float)(modelReader.ReadSByte()) / 255, (float)(modelReader.ReadSByte()) / 255 };
        case ModelVertexFormat.UINT32:
          return modelReader.ReadUInt32();
        case ModelVertexFormat.NONE:
          return null;
        default:
          if(unhandledVertexFormats.Add(format)) {
            Console.Out.WriteLine("Unhandled vertex format {0:X}!", format);
          }
          return null;
      }
    }

    public Model(Stream modelStream) {
      unhandledVertexFormats = new HashSet<ModelVertexFormat>();
      unhandledVertexTypes = new HashSet<ModelVertexType>();
      using(BinaryReader modelReader = new BinaryReader(modelStream)) {
        modelStream.Seek(336L, SeekOrigin.Begin);
        vertexBufferCount = modelReader.ReadByte();
        indiceBufferCount = modelReader.ReadByte();
        modelStream.ReadByte();
        submeshCount = modelReader.ReadByte();

        modelStream.Seek(360L, SeekOrigin.Begin);
        ulong submeshInfoPtr = modelReader.ReadUInt64();
        ulong vertexInfoPtr = modelReader.ReadUInt64();
        ulong indiceInfoPtr = modelReader.ReadUInt64();

			  modelStream.Seek(172L, SeekOrigin.Begin);
			  boneCount = modelReader.ReadUInt16();
			  modelReader.ReadInt16();
			  modelReader.ReadInt16();
        uint boneIndices = modelReader.ReadUInt16();

        modelStream.Seek(152L, SeekOrigin.Begin);
        modelStream.Seek((long)modelReader.ReadUInt64(), SeekOrigin.Begin);
        boneLookup = new ushort[boneIndices];
        for(int i = 0; i < boneIndices; ++i) {
          boneLookup[i] = modelReader.ReadUInt16();
        }
        modelStream.Seek(80L, SeekOrigin.Begin);
        modelStream.Seek((long)modelReader.ReadUInt64(), SeekOrigin.Begin);
        boneHierarchy = new short[boneCount];
        for(int i = 0; i < boneCount; ++i) { 
          modelStream.Seek(4L, SeekOrigin.Current);
          boneHierarchy[i] = modelReader.ReadInt16();
        }

        boneData = new float[boneCount][];
        modelStream.Seek(88L, SeekOrigin.Begin);
        modelStream.Seek((long)modelReader.ReadUInt64(), SeekOrigin.Begin);
        for(int i = 0; i < boneCount; ++i) {
          modelStream.Seek(48L, SeekOrigin.Current);
          boneData[i] = new float[4] { modelReader.ReadSingle(), modelReader.ReadSingle(), modelReader.ReadSingle(), modelReader.ReadSingle() };
        }

        submeshes = new ModelSubmesh[submeshCount];
        modelStream.Seek((long)submeshInfoPtr, SeekOrigin.Begin);
        for(int i = 0; i < submeshCount; ++i) {
          submeshes[i] = modelReader.Read<ModelSubmesh>();
        }

        vertexBuffers = new ModelVertexBuffer[vertexBufferCount];
        modelStream.Seek((long)vertexInfoPtr, SeekOrigin.Begin);
        uint[] uvChain = new uint[vertexBufferCount];
        for(int i = 0; i < vertexBufferCount; ++i) {
          vertexBuffers[i] = modelReader.Read<ModelVertexBuffer>();
        }
        vertexElements = new ModelVertexElement[vertexBufferCount][][];
        vertexStrideStream = new object[vertexBufferCount][][][];
        for(int i = 0; i < vertexBufferCount; ++i) {
          vertexElements[i] = new ModelVertexElement[2][] { new ModelVertexElement[vertexBuffers[i].strideStream1], new ModelVertexElement[vertexBuffers[i].strideStream2] };
          modelStream.Seek((long)vertexBuffers[i].vertexElementPtr, SeekOrigin.Begin);
          vertexStrideStream[i] = new object[2][][] { new object[vertexBuffers[i].inputElementCount][], new object[vertexBuffers[i].inputElementCount][]};
          int[] ind = new int[2] { 0, 0 };
          for(int j = 0; j < vertexBuffers[i].vertexElements; ++j) {
            ModelVertexElement tmp = modelReader.Read<ModelVertexElement>();
            vertexElements[i][tmp.stream][ind[tmp.stream]] = tmp;
            ind[tmp.stream] += 1;
          };
          
          modelStream.Position = (long)vertexBuffers[i].stream1Ptr;
          for(int j = 0; j < vertexBuffers[i].inputElementCount; ++j) {
            long start = modelStream.Position;
            vertexStrideStream[i][0][j] = new object[vertexElements[i][0].Length];
            for(int o = 0; o < vertexElements[i][0].Length; ++o) {
              ModelVertexElement elem = vertexElements[i][0][o];
              modelStream.Position = start + elem.offset;
              vertexStrideStream[i][0][j][o] = ReadStrideValue(elem.format, modelReader);
            }
            modelStream.Position = start + vertexBuffers[i].strideStream1;
          }

          modelStream.Position = (long)vertexBuffers[i].stream2Ptr;
          for(int j = 0; j < vertexBuffers[i].inputElementCount; ++j) {
            long start = modelStream.Position;
            vertexStrideStream[i][1][j] = new object[vertexElements[i][1].Length];
            for(int o = 0; o < vertexElements[i][1].Length; ++o) {
              ModelVertexElement elem = vertexElements[i][1][o];
              modelStream.Position = start + elem.offset;
              vertexStrideStream[i][1][j][o] = ReadStrideValue(elem.format, modelReader);
            }
            modelStream.Position = start + vertexBuffers[i].strideStream2;
          }
        }

        indiceBuffers = new ModelIndiceBuffer[indiceBufferCount];
        modelStream.Seek((long)indiceInfoPtr, SeekOrigin.Begin);
        for(int i = 0; i < indiceBufferCount; ++i) {
          indiceBuffers[i] = modelReader.Read<ModelIndiceBuffer>();
        }
        
        uvs = new ModelUV[submeshCount][];
        faces = new ModelIndice[submeshCount][];
        vertices = new ModelVertex[submeshCount][];
        normals = new ModelVertex[submeshCount][];
        bones = new ModelBoneData[submeshCount][];

        for(int i = 0; i < submeshCount; ++i) {
          ModelSubmesh submesh = submeshes[i];
          ModelIndiceBuffer indiceBuffer = indiceBuffers[submesh.indexBufferIndex];
          ModelVertexBuffer vertexBuffer = vertexBuffers[submesh.vertexBufferIndex];
          uint sz = 0;
          ModelIndice[] indices = new ModelIndice[submesh.indexEnd / 3];
          modelStream.Seek((long)indiceBuffer.stream1Ptr + submesh.indexStart * 2, SeekOrigin.Begin);
          for(int j = 0; j < indices.Length; ++j) {
            indices[j] = modelReader.Read<ModelIndice>();
            sz = Math.Max(sz, indices[j].v1);
            sz = Math.Max(sz, indices[j].v2);
            sz = Math.Max(sz, indices[j].v3);
          }
          sz += 1;
          ModelUV[] uv = new ModelUV[sz];
          ModelVertex[] vertex = new ModelVertex[sz];
          ModelBoneData[] bone = new ModelBoneData[sz];
          ModelVertex[] normal = new ModelVertex[sz];

          ModelVertexElement[][] elements = vertexElements[submesh.vertexBufferIndex];

          for(int j = 0; j < sz; ++j) {
            long offset = submesh.vertexStart + j;
            for(int k = 0; k < elements[0].Length; ++k) {
              ModelVertexElement element = elements[0][k];
              if(element.format == ModelVertexFormat.NONE) {
                continue;
              }
              object value = vertexStrideStream[submesh.vertexBufferIndex][0][offset][k];
              switch(element.type) {
                case ModelVertexType.UV:
                  uv[j] = new ModelUV { u = ((Half[])value)[0], v = ((Half[])value)[0] };
                  break;
                case ModelVertexType.NORMAL:
                  normal[j] = new ModelVertex { x = ((float[])value)[0], y = ((float[])value)[1], z = ((float[])value)[2] };
                  break;
                case ModelVertexType.POSITION:
                  vertex[j] = new ModelVertex { x = ((float[])value)[0], y = ((float[])value)[1], z = ((float[])value)[2] };
                  break;
                case ModelVertexType.BONE_INDEX:
                  bone[j].boneIndex = (byte[])value;
                  break;
                case ModelVertexType.BONE_WEIGHT:
                  bone[j].boneWeight = (float[])value;
                  break;
                default:
                  if(unhandledVertexTypes.Add(element.type)) {
                    Console.Out.WriteLine("Unhandled vertex type {0:X}!", element.type);
                  }
                  break;
              }
            }
          }
          for(int j = 0; j < sz; ++j) {
            long offset = submesh.vertexStart + j;
            for(int k = 0; k < vertexBuffer.strideStream2; ++k) {
              ModelVertexElement element = vertexElements[submesh.vertexBufferIndex][1][k];
              if(element.format == ModelVertexFormat.NONE) {
                continue;
              }
              object value = vertexStrideStream[submesh.vertexBufferIndex][1][offset][k];
              switch(element.type) {
                case ModelVertexType.UV:
                  uv[j] = new ModelUV { u = ((Half[])value)[0], v = ((Half[])value)[0] };
                  break;
                case ModelVertexType.NORMAL:
                  normal[j] = new ModelVertex { x = ((float[])value)[0], y = ((float[])value)[1], z = ((float[])value)[2] };
                  break;
                case ModelVertexType.POSITION:
                  vertex[j] = new ModelVertex { x = ((float[])value)[0], y = ((float[])value)[1], z = ((float[])value)[2] };
                  break;
                case ModelVertexType.BONE_INDEX:
                  bone[j].boneIndex = (byte[])value;
                  break;
                case ModelVertexType.BONE_WEIGHT:
                  bone[j].boneWeight = (float[])value;
                  break;
                default:
                  if(unhandledVertexTypes.Add(element.type)) {
                    Console.Out.WriteLine("Unhandled vertex type {0:X}!", element.type);
                  }
                  break;
              }
            }
            offset += vertexBuffer.strideStream2;
          }
          uvs[i] = uv;
          vertices[i] = vertex;
          faces[i] = indices;
          bones[i] = bone;
          normals[i] = normal;
        }
      }
    }
  }
}
