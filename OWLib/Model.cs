using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib.Types;

namespace OWLib {
  public class Model {
    private byte vertexBufferCount;
    private byte indiceBufferCount;
    private byte submeshCount;
    private ushort boneCount;

    private short[] boneHierarchy;
    private ushort[] boneLookup;
    private float[][] boneData;
    private ModelSubmesh[] submeshes;
    private ModelVertexBuffer[] vertexBuffers;
    private ModelIndiceBuffer[] indiceBuffers;
    private ModelUV[][] uvs;
    private ModelIndice[][] faces;
    private ModelVertex[][] vertices;
    private ModelBoneData[][] bones;

    public short[] BoneHierarchy => boneHierarchy;
    public ushort[] BoneLookup => boneLookup;
    public float[][] BoneData => boneData;
    public ModelSubmesh[] Submeshes => submeshes;
    public ModelUV[][] UVs => uvs;
    public ModelIndice[][] Faces => faces;
    public ModelVertex[][] Vertices => vertices;
    public ModelBoneData[][] Bones => bones;

    public Model(Stream modelStream) {
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
        for(int i = 0; i < vertexBufferCount; ++i) {
          modelStream.Seek((long)vertexBuffers[i].chainlinkPtr, SeekOrigin.Begin);
          for(int j = 0; j < vertexBuffers[i].formatStream1; ++j) {
            ModelChainlink tmp = modelReader.Read<ModelChainlink>();
            if(tmp.type == 9) {
              uvChain[i] = tmp.offset;
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
          uint uvOffset = uvChain[submesh.vertexBufferIndex];
          uint uvNext = vertexBuffer.strideStream2 - uvOffset - 4;
          ModelUV[] uv = new ModelUV[sz];
          modelStream.Seek((long)vertexBuffer.stream2Ptr + submesh.vertexStart * vertexBuffer.strideStream2, SeekOrigin.Begin);
          for(int j = 0; j < sz; ++j) {
            modelStream.Seek(uvOffset, SeekOrigin.Current);
            uv[j] = modelReader.Read<ModelUVShort>().ToModelUV();
            modelStream.Seek(uvNext, SeekOrigin.Current);
          }
          modelStream.Seek((long)vertexBuffer.stream1Ptr + submesh.vertexStart * vertexBuffer.strideStream1, SeekOrigin.Begin);
          ModelVertex[] vertex = new ModelVertex[sz];
          ModelBoneData[] bone = new ModelBoneData[sz];
          for(int j = 0; j < sz; ++j) {
            vertex[j] = modelReader.Read<ModelVertex>();
            if(vertexBuffer.strideStream1 > 12) {
              bone[j] = modelReader.Read<ModelBoneData>();
            }
          }
          uvs[i] = uv;
          vertices[i] = vertex;
          faces[i] = indices;
          bones[i] = bone;
        }
      }
    }
  }
}
