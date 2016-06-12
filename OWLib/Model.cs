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
    private HashSet<long> unhandledVertexLayers;
    private short[] boneHierarchy;

    private ushort[] boneLookup;
    private uint[] boneIDs;
    private OpenTK.Matrix4[] boneData;
    private OpenTK.Matrix4[] boneData2;
    private OpenTK.Matrix3x4[] poseData;
    private OpenTK.Matrix3x4[] poseData2;
    private ulong[] materialKeys;
    private ModelSubmesh[] submeshes;
    private ModelVertexBuffer[] vertexBuffers;
    private ModelIndiceBuffer[] indiceBuffers;
    private ModelUV[][][] uvs;
    private ModelIndice[][] faces;
    private ModelVertex[][] vertices;
    private ModelVertex[][] normals;
    private ModelBoneData[][] bones;
    private ModelVertexElement[][][] vertexElements;
    private byte[] uvLayerCount;
    private object[][][][] vertexStrideStream;
    private ModelAttachmentPoint[] attachmentPoints;

    public ModelHeader Header => header;
    public short[] BoneHierarchy => boneHierarchy;
    public ushort[] BoneLookup => boneLookup;
    public uint[] BoneIDs => boneIDs;
    public OpenTK.Matrix4[] BoneData => boneData;
    public OpenTK.Matrix4[] BoneData2 => boneData2;
    public OpenTK.Matrix3x4[] PoseData => poseData;
    public OpenTK.Matrix3x4[] PoseData2 => poseData2;
    public ulong[] MaterialKeys => materialKeys;
    public ModelSubmesh[] Submeshes => submeshes;
    public ModelVertexBuffer[] VertexBuffers => vertexBuffers;
    public ModelIndiceBuffer[] IndiceBuffers => indiceBuffers;
    public ModelUV[][][] UVs => uvs;
    public ModelIndice[][] Faces => faces;
    public ModelVertex[][] Vertices => vertices;
    public ModelVertex[][] Normals => normals;
    public ModelBoneData[][] Bones => bones;
    public ModelVertexElement[][][] VertexElements => vertexElements;
    public object[][][] VertexStrideStream => vertexStrideStream;
    public ModelAttachmentPoint[] AttachmentPoints => attachmentPoints;

    public object ReadStrideValue(ModelVertexFormat format, BinaryReader modelReader) {
      switch(format) {
        case ModelVertexFormat.SINGLE_3:
          return new float[3] { modelReader.ReadSingle(), modelReader.ReadSingle(), modelReader.ReadSingle() };
        case ModelVertexFormat.HALF_2:
          return new ushort[2] { modelReader.ReadUInt16(), modelReader.ReadUInt16() };
        case ModelVertexFormat.UINT8_4:
          return new byte[4] { modelReader.ReadByte(), modelReader.ReadByte(), modelReader.ReadByte(), modelReader.ReadByte() };
        case ModelVertexFormat.UINT8_UNORM4:
          return new float[4] { (float)(modelReader.ReadByte()) / 255f, (float)(modelReader.ReadByte())  / 255f, (float)(modelReader.ReadByte())  / 255f, (float)(modelReader.ReadByte())  / 255f };
        case ModelVertexFormat.UINT8_SNORM4:
          return new float[4] { (float)(modelReader.ReadSByte()) / 255f, (float)(modelReader.ReadSByte()) / 255f, (float)(modelReader.ReadSByte()) / 255f, (float)(modelReader.ReadSByte()) / 255f };
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

    public struct AttachmentPoint {
      public OpenTK.Vector3[] points;
      public int[] indices;
    }

    public AttachmentPoint[] CreateAttachmentPoints() {
      AttachmentPoint[] ret = new AttachmentPoint[header.unkStruct2Count];
      for(int i = 0; i < header.unkStruct2Count; ++i) {
        int[] indices = new int[36] {
          0, 1, 2, 2, 3, 0, // front
          3, 2, 6, 6, 7, 3, // top
          7, 6, 5, 5, 4, 7, // back
          4, 0, 3, 3, 7, 4, // left
          0, 1, 5, 5, 4, 0, // bottom
          1, 5, 6, 6, 2, 1  // right
        };

        OpenTK.Vector3[] points = new OpenTK.Vector3[8] {
          new OpenTK.Vector3(-1.0f, -1.0f,  1.0f), // 0
          new OpenTK.Vector3( 1.0f, -1.0f,  1.0f), // 1
          new OpenTK.Vector3( 1.0f,  1.0f,  1.0f), // 2
          new OpenTK.Vector3(-1.0f,  1.0f,  1.0f), // 3
          new OpenTK.Vector3(-1.0f, -1.0f, -1.0f), // 4
          new OpenTK.Vector3( 1.0f, -1.0f, -1.0f), // 5
          new OpenTK.Vector3( 1.0f,  1.0f, -1.0f), // 6
          new OpenTK.Vector3(-1.0f,  1.0f, -1.0f)  // 7
        };

        OpenTK.Matrix4 matrix = attachmentPoints[i].matrix.ToOpenTK();
        OpenTK.Vector3 translation = matrix.ExtractTranslation();
        OpenTK.Quaternion rotation = matrix.ExtractRotation(false);
        for(int j = 0; j < 8; ++j) {
          points[j].X *= 0.1f;
          points[j].Y *= 0.1f;
          points[j].Z *= 0.1f;
          OpenTK.Vector3.Transform(points[j], rotation);
          points[j].X += translation.X;
          points[j].Y += translation.Y;
          points[j].Z += translation.Z;
        }

        ret[i] = new AttachmentPoint { points = points, indices = indices };
      }
      return ret;
    }

    public Model(Stream modelStream) {
      unhandledVertexFormats = new HashSet<ModelVertexFormat>();
      unhandledVertexTypes = new HashSet<ModelVertexType>();
      unhandledVertexLayers = new HashSet<long>();
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

        modelStream.Seek((long)header.boneIdPtr, SeekOrigin.Begin);
        boneIDs = new uint[boneCount];
        for(int i = 0; i < boneCount; ++i) {
          boneIDs[i] = modelReader.ReadUInt32();
        }

        modelStream.Seek((long)header.materialBufferPtr, SeekOrigin.Begin);
        materialKeys = new ulong[Math.Max(header.materialACount, header.materialBCount)];
        for(int i = 0; i < materialKeys.Length; ++i) {
          materialKeys[i] = modelReader.ReadUInt64();
        }
        
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
        uvLayerCount = new byte[vertexBufferCount];
        for(int i = 0; i < vertexBufferCount; ++i) {
          vertexElements[i] = new ModelVertexElement[2][] { new ModelVertexElement[vertexBuffers[i].strideStream1], new ModelVertexElement[vertexBuffers[i].strideStream2] };
          modelStream.Seek((long)vertexBuffers[i].vertexElementPtr, SeekOrigin.Begin);
          vertexStrideStream[i] = new object[2][][] { new object[vertexBuffers[i].inputElementCount][], new object[vertexBuffers[i].inputElementCount][]};
          uvLayerCount[i] = 1;
          int[] ind = new int[2] { 0, 0 };
          for(int j = 0; j < vertexBuffers[i].vertexElements; ++j) {
            ModelVertexElement tmp = modelReader.Read<ModelVertexElement>();
            vertexElements[i][tmp.stream][ind[tmp.stream]] = tmp;
            if(tmp.type == ModelVertexType.UV) {
              uvLayerCount[i] = (byte)Math.Max(uvLayerCount[i], tmp.index + 1);
            }
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
        
        uvs = new ModelUV[submeshCount][][];
        faces = new ModelIndice[submeshCount][];
        vertices = new ModelVertex[submeshCount][];
        normals = new ModelVertex[submeshCount][];
        bones = new ModelBoneData[submeshCount][];

        for(int i = 0; i < submeshCount; ++i) {
          ModelSubmesh submesh = submeshes[i];
          ModelVertexElement[][] elements = vertexElements[submesh.vertexBufferIndex];
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
          ModelUV[][] uv = new ModelUV[uvLayerCount[submesh.vertexBufferIndex]][];
          for(int j = 0; j < uvLayerCount[submesh.vertexBufferIndex]; ++j) {
            uv[j] = new ModelUV[submesh.vertexCount];
          }
          ModelVertex[] vertex = new ModelVertex[submesh.vertexCount];
          ModelBoneData[] bone = new ModelBoneData[submesh.vertexCount];
          ModelVertex[] normal = new ModelVertex[submesh.vertexCount];

          for(int w = 0; w < vertexStrideStream[submesh.vertexBufferIndex].Length; ++w) {
            for(int j = 0; j < submesh.vertexCount; ++j) {
              long offset = submesh.vertexStart + indiceT[j];
              for(int k = 0; k < elements[w].Length; ++k) {
                ModelVertexElement element = elements[w][k];
                if(element.format == ModelVertexFormat.NONE) {
                  break;
                }
                object value = vertexStrideStream[submesh.vertexBufferIndex][w][offset][k];
                if(element.index > 0 && element.type != ModelVertexType.UV) {
                  long idx = ((long)element.type << 1) + element.index;
                  if(unhandledVertexLayers.Add(idx)) {
                    Console.Out.WriteLine("Unhandled vertex layer {0:X} for type {1:X}!", element.index, element.type);
                  }
                }
                switch(element.type) {
                  case ModelVertexType.UV:
                    ushort[] t = (ushort[])value;
                    uv[element.index][j] = new ModelUV { u = Half.ToHalf(t[0]), v = Half.ToHalf(t[1]) };
                    break;
                  case ModelVertexType.NORMAL:
                    normal[j] = new ModelVertex { x = ((float[])value)[0], y = ((float[])value)[1], z = ((float[])value)[2] };
                    break;
                  case ModelVertexType.POSITION:
                    vertex[j] = new ModelVertex { x = ((float[])value)[0], y = ((float[])value)[1], z = ((float[])value)[2] };
                    break;
                  case ModelVertexType.BONE_INDEX:
                    byte[] tmp = (byte[])value;
                    bone[j].boneIndex = new ushort[tmp.Length];
                    for(int l = 0; l < tmp.Length; ++l) {
                      bone[j].boneIndex[l] = (ushort)(tmp[l] + submesh.boneOffset);
                    }
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
          }
          indiceT.Clear();
          uvs[i] = uv;
          vertices[i] = vertex;
          faces[i] = indices;
          bones[i] = bone;
          normals[i] = normal;
        }

        modelStream.Seek((long)header.unkStruct2Ptr, SeekOrigin.Begin);
        attachmentPoints = new ModelAttachmentPoint[header.unkStruct2Count];
        for(int i = 0; i < header.unkStruct2Count; ++i) {
          attachmentPoints[i] = modelReader.Read<ModelAttachmentPoint>();
        }
      }
    }
  }
}
