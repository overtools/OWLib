using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct MeshDescriptor {
    public uint vertexCount;
    public byte submeshDescriptorCount;
    public byte unkCount1;
    public byte unkCount2;
    public byte unkCount3;
    public byte materialCount;
    public byte unkCount4;
    public byte vertexBufferDescriptorCount;
    public byte indexBufferDescriptorCount;
    fixed float unk5[13];
    fixed uint unk6[4];
    fixed float unk7[4];
    fixed uint unk8[4];
    public long vertexBufferDesciptorPointer;
    public long indexBufferDescriptorPointer;
    public long submeshDescriptorPointer;
    public long unkADescriptorPointer;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct VertexBufferDescriptor {
    public uint vertexCount;
    public uint unk1;
    public byte strideStream1;
    public byte strideStream2;
    public byte vertexElementDescriptorCount;
    public byte unk2;
    public uint unk3;
    public long vertexElementDescriptorPointer;
    public long dataStream1Pointer;
    public long dataStream2Pointer;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct IndexBufferDescriptor {
    public uint indexCount;
    public uint format;
    public long dataStreamPointer;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct SubmeshDescriptor {
    fixed float unk1[10];
    public long unk2Index;
    public float unk3;
    public uint vertexStart;
    public ushort indexStart;
    ushort pad1;
    fixed uint unk4[3];
    public ushort indexCount;
    public ushort indicesToDraw;
    public ushort verticesToDraw;
    public ushort boneIdOffset;
    public byte indexBuffer;
    fixed byte pad3[7];
    public byte vertexBuffer;
    public SubmeshFlags flags;
    public byte material;
    public byte lod;
    public uint unk5;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct VertexElementDescriptor {
    public SemanticType type;
    public byte index;
    public SemanticFormat format;
    public byte stream;
    public ushort classification;
    public ushort offset;
  }

  public enum SemanticType : byte {
    POSITION = 0x0,
    NORMAL = 0x1,
    COLOR = 0x2,
    TANGENT = 0x3,
    BONE_INDEX = 0x4,
    BONE_WEIGHT = 0x5,
    UNKNOWN_1 = 0x6,
    UNKNOWN_2 = 0x7,
    UNKNOWN_3 = 0x8,
    UV = 0x9,
    ID = 0x10
  }

  public enum SemanticFormat : byte {
    NONE = 0x0,
    SINGLE_3 = 0x2,
    HALF_2 = 0x4,
    UINT8_4 = 0x6,
    UINT8_UNORM4 = 0x8,
    UINT8_SNORM4 = 0x9,
    UINT32 = 0xC
  }

  public enum SubmeshFlags : byte {
    UNK1 = 1,
    UNK2 = 2,
    UNK3 = 4,
    UNK4 = 8,
    COLLISION_MESH = 16,
    UNK6 = 32,
    UNK7 = 64,
    UNK8 = 128
  }
}
