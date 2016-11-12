using System;
using System.Collections.Generic;
using System.IO;

namespace OWLib.Types.Chunk {
  public class MNRM : IChunk {
    public string Identifier
    {
      get
      {
        return "MNRM"; // MRNM
      }
    }

    public string RootIdentifier
    {
      get
      {
        return "LDOM"; // MODL
      }
    }

    private static HashSet<SemanticFormat> unhandledSemanticFormats = new HashSet<SemanticFormat>();

    public MeshDescriptor Mesh;
    public VertexBufferDescriptor[] VertexBuffers;
    public IndexBufferDescriptor[] IndexBuffers;
    public VertexElementDescriptor[][] VertexElements;
    public SubmeshDescriptor[] Submeshes;

    public ModelVertex[][] Vertices;
    public ModelIndice[][] Indices;
    public ModelUV[][][] TextureCoordinates;
    public ModelBoneData[][] Bones;

    public void Parse(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        Mesh = reader.Read<MeshDescriptor>();

        VertexBuffers = new VertexBufferDescriptor[Mesh.vertexBufferDescriptorCount];
        VertexElements = new VertexElementDescriptor[Mesh.vertexBufferDescriptorCount][];
        IndexBuffers = new IndexBufferDescriptor[Mesh.indexBufferDescriptorCount];
        Submeshes = new SubmeshDescriptor[Mesh.submeshDescriptorCount];

        ParseVBO(reader);
        ParseIBO(reader);
        ParseSubmesh(reader);
        GenerateMeshes(reader);
      }
    }

    #region Parser Subfunctions
    public void ParseVBO(BinaryReader reader) {
      reader.BaseStream.Position = Mesh.vertexBufferDesciptorPointer;
      for(int i = 0; i < Mesh.vertexBufferDescriptorCount; ++i) {
        VertexBufferDescriptor descriptor = reader.Read<VertexBufferDescriptor>();
        VertexBuffers[i] = descriptor;
      }
      for(int i = 0; i < Mesh.vertexBufferDescriptorCount; ++i) {
        VertexElements[i] = ParseVBE(reader, VertexBuffers[i]);
      }
    }

    public void ParseIBO(BinaryReader reader) {
      reader.BaseStream.Position = Mesh.indexBufferDescriptorPointer;
      for(int i = 0; i < Mesh.indexBufferDescriptorCount; ++i) {
        IndexBufferDescriptor descriptor = reader.Read<IndexBufferDescriptor>();
        IndexBuffers[i] = descriptor;
      }
    }

    public void ParseSubmesh(BinaryReader reader) {
      reader.BaseStream.Position = Mesh.submeshDescriptorPointer;
      for(int i = 0; i < Mesh.submeshDescriptorCount; ++i) {
        SubmeshDescriptor submesh = reader.Read<SubmeshDescriptor>();
        Submeshes[i] = submesh;
      }
    }

    public VertexElementDescriptor[] ParseVBE(BinaryReader reader, VertexBufferDescriptor descriptor) {
      reader.BaseStream.Position = descriptor.vertexElementDescriptorPointer;
      VertexElementDescriptor[] elements = new VertexElementDescriptor[descriptor.vertexElementDescriptorCount];
      for(int i = 0; i < descriptor.vertexElementDescriptorCount; ++i) {
        elements[i] = reader.Read<VertexElementDescriptor>();
      }
      return elements;
    }

    // split VBE by stream
    public VertexElementDescriptor[][] SplitVBE(VertexElementDescriptor[] input) {
      VertexElementDescriptor[][] elements = new VertexElementDescriptor[2][];

      // pass 1
      byte[] sizes = new byte[2] { 0, 0 };
      for(int i = 0; i < input.Length; ++i) {
        sizes[input[i].stream] += 1;
      }

      // pass 2
      elements[0] = new VertexElementDescriptor[sizes[0]];
      elements[1] = new VertexElementDescriptor[sizes[1]];
      sizes = new byte[2] { 0, 0 };
      for(int i = 0; i < input.Length; ++i) {
        byte stream = input[i].stream;
        elements[stream][sizes[stream]] = input[i];
        sizes[stream] += 1;
      }
      return elements;
    }
    #endregion

    public object ReadElement(SemanticFormat format, BinaryReader reader) {
      switch(format) {
        case SemanticFormat.SINGLE_3:
          return new float[3] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
        case SemanticFormat.HALF_2:
          return new ushort[2] { reader.ReadUInt16(), reader.ReadUInt16() };
        case SemanticFormat.UINT8_4:
          return new byte[4] { reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte() };
        case SemanticFormat.UINT8_UNORM4:
          return new float[4] { reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f };
        case SemanticFormat.UINT8_SNORM4:
          return new float[4] { reader.ReadSByte() / 255f, reader.ReadSByte() / 255f, reader.ReadSByte() / 255f, reader.ReadSByte() / 255f };
        case SemanticFormat.NONE:
          return null;
        case SemanticFormat.UINT32:
          return reader.ReadUInt32();
        default:
          if(unhandledSemanticFormats.Add(format)) {
            Console.Out.WriteLine("Unhandled Semantic Format {0:X}!", format);
          }
          return null;
      }
    }

    public void GenerateMeshes(BinaryReader reader) {
      // TODO
    }
  }
}
