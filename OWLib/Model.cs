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

    private ModelHeader header;
    private HashSet<ModelVertexType> unhandledVertexTypes;
    private HashSet<ModelVertexFormat> unhandledVertexFormats;
    private short[] boneHierarchy;
    private ushort[] boneLookup;
    private OpenTK.Matrix4[] boneData;
    private OpenTK.Matrix4[] boneData2;
    private OpenTK.Matrix3x4[] poseData;
    private OpenTK.Matrix3x4[] poseData2;
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

    public ModelHeader Header => header;
    public short[] BoneHierarchy => boneHierarchy;
    public ushort[] BoneLookup => boneLookup;
    public OpenTK.Matrix4[] BoneData => boneData;
    public OpenTK.Matrix4[] BoneData2 => boneData2;
    public OpenTK.Matrix3x4[] PoseData => poseData;
    public OpenTK.Matrix3x4[] PoseData2 => poseData2;
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
          return new ushort[2] { modelReader.ReadUInt16(), modelReader.ReadUInt16() };
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
        header = modelReader.Read<ModelHeader>();
        
        vertexBufferCount = header.vertexBufferCount;
        indiceBufferCount = header.indiceBufferCount;
        submeshCount = header.submeshCount;
        
        ulong submeshInfoPtr = header.submeshBufferPtr;
        ulong vertexInfoPtr = header.vertexBufferPtr;
        ulong indiceInfoPtr = header.indiceBufferPtr;
        
        boneCount = header.boneCount;
        uint boneIndices = header.boneCount3;
        
        modelStream.Seek((long)header.boneEx2Ptr, SeekOrigin.Begin);
        boneLookup = new ushort[boneIndices];
        for(int i = 0; i < boneIndices; ++i) {
          boneLookup[i] = modelReader.ReadUInt16();
        }

        modelStream.Seek((long)header.boneHierarchy1Ptr, SeekOrigin.Begin);
        boneHierarchy = new short[boneCount];
        for(int i = 0; i < boneCount; ++i) { 
          modelStream.Seek(4L, SeekOrigin.Current);
          boneHierarchy[i] = modelReader.ReadInt16();
        }

        boneData = new OpenTK.Matrix4[boneCount];
        modelStream.Seek((long)header.boneMatrix1Ptr, SeekOrigin.Begin);
        for(int i = 0; i < boneCount; ++i) {
          boneData[i] = modelReader.Read<Matrix4B>().ToOpenTK();
        }

        boneData2 = new OpenTK.Matrix4[boneCount];
        modelStream.Seek((long)header.boneMatrix2Ptr, SeekOrigin.Begin);
        for(int i = 0; i < boneCount; ++i) {
          boneData2[i] = modelReader.Read<Matrix4B>().ToOpenTK();
        }

        poseData = new OpenTK.Matrix3x4[boneCount];
        modelStream.Seek((long)header.boneMatrix3Ptr, SeekOrigin.Begin);
        for(int i = 0; i < boneCount; ++i) {
          poseData[i] = modelReader.Read<Matrix3x4B>().ToOpenTK();
        }

        poseData2 = new OpenTK.Matrix3x4[boneCount];
        modelStream.Seek((long)header.boneMatrix4Ptr, SeekOrigin.Begin);
        for(int i = 0; i < boneCount; ++i) {
          poseData2[i] = modelReader.Read<Matrix3x4B>().ToOpenTK();
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
          
          long[] ptrs    = new long[2] { (long)vertexBuffers[i].stream1Ptr, (long)vertexBuffers[i].stream2Ptr };
          byte[] stridel = new byte[2] { vertexBuffers[i].strideStream1, vertexBuffers[i].strideStream2 };

          for(int w = 0; w < ptrs.Length; ++w) {
            modelStream.Position = ptrs[w];
            for(int j = 0; j < vertexBuffers[i].inputElementCount; ++j) {
              long start = modelStream.Position;
              vertexStrideStream[i][w][j] = new object[vertexElements[i][w].Length];
              for(int o = 0; o < vertexElements[i][w].Length; ++o) {
                ModelVertexElement elem = vertexElements[i][w][o];
                modelStream.Position = start + elem.offset;
                vertexStrideStream[i][w][j][o] = ReadStrideValue(elem.format, modelReader);
              }
              modelStream.Position = start + stridel[w];
            }
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
          ModelIndice[] indices = new ModelIndice[submesh.indiceCount / 3];
          modelStream.Seek((long)indiceBuffer.stream1Ptr + submesh.indexStart * 2, SeekOrigin.Begin);
          List<ushort> indiceT = new List<ushort>(submesh.vertexCount);
          for(int j = 0; j < indices.Length; ++j) {
            ModelIndice ind = modelReader.Read<ModelIndice>();
            int v1 = indiceT.IndexOf(ind.v1);
            if(v1 == -1) {
              v1 = indiceT.Count;
              indiceT.Add(ind.v1);
            }
            int v2 = indiceT.IndexOf(ind.v2);
            if(v2 == -1) {
              v2 = indiceT.Count;
              indiceT.Add(ind.v2);
            }
            int v3 = indiceT.IndexOf(ind.v3);
            if(v3 == -1) {
              v3 = indiceT.Count;
              indiceT.Add(ind.v3);
            }
            indices[j] = new ModelIndice { v1 = (ushort)v1, v2 = (ushort)v2, v3 = (ushort)v3 };
          }
          ModelUV[] uv = new ModelUV[submesh.vertexCount];
          ModelVertex[] vertex = new ModelVertex[submesh.vertexCount];
          ModelBoneData[] bone = new ModelBoneData[submesh.vertexCount];
          ModelVertex[] normal = new ModelVertex[submesh.vertexCount];

          ModelVertexElement[][] elements = vertexElements[submesh.vertexBufferIndex];
          HashSet<int> uvj = new HashSet<int>();
          HashSet<int> noj = new HashSet<int>();
          HashSet<int> vej = new HashSet<int>();
          HashSet<int> bij = new HashSet<int>();
          HashSet<int> bwj = new HashSet<int>();
          for(int w = 0; w < vertexStrideStream[submesh.vertexBufferIndex].Length; ++w) {
            for(int j = 0; j < submesh.vertexCount; ++j) {
              long offset = submesh.vertexStart + indiceT[j];
              for(int k = 0; k < elements[w].Length; ++k) {
                ModelVertexElement element = elements[w][k];
                if(element.format == ModelVertexFormat.NONE) {
                  continue;
                }
                object value = vertexStrideStream[submesh.vertexBufferIndex][w][offset][k];
                switch(element.type) {
                  case ModelVertexType.UV:
                    if(!uvj.Add(j)) {
                      break;
                    }
                    ushort[] t = (ushort[])value;
                    uv[j] = new ModelUV { u = Half.ToHalf(t[0]), v = Half.ToHalf(t[1]) };
                    break;
                  case ModelVertexType.NORMAL:
                    if(!noj.Add(j)) {
                      break;
                    }
                    normal[j] = new ModelVertex { x = ((float[])value)[0], y = ((float[])value)[1], z = ((float[])value)[2] };
                    break;
                  case ModelVertexType.POSITION:
                    if(!vej.Add(j)) {
                      break;
                    }
                    vertex[j] = new ModelVertex { x = ((float[])value)[0], y = ((float[])value)[1], z = ((float[])value)[2] };
                    break;
                  case ModelVertexType.BONE_INDEX:
                    if(!bij.Add(j)) {
                      break;
                    }
                    byte[] tmp = (byte[])value;
                    bone[j].boneIndex = new ushort[tmp.Length];
                    for(int l = 0; l < tmp.Length; ++l) {
                      bone[j].boneIndex[l] = (ushort)(tmp[l] + submesh.boneOffset);
                    }
                    break;
                  case ModelVertexType.BONE_WEIGHT:
                    if(!bwj.Add(j)) {
                      break;
                    }
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
          }
          indiceT.Clear();
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
