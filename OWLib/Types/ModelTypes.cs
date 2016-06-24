using System;
using System.Runtime.InteropServices;

namespace OWLib.Types {
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public unsafe struct ModelHeader {
    public fixed float boundingBox[16];
    public uint blobPtr;
    public uint unkStruct1Ptr;
    public ulong eof;
    public ulong boneHierarchy1Ptr;
    public ulong boneMatrix1Ptr;
    public ulong boneMatrix2Ptr;
    public ulong boneMatrix3Ptr;
    public ulong boneMatrix4Ptr;
    public ulong unkBoneUshort;
    public ulong boneIdPtr;
    public ulong boneBindataPtr;
    public ulong boneEx1Ptr;
    public ulong boneEx2Ptr;
    public ulong boneHierarchy2Ptr;
    public uint magic;
    public ushort boneCount;
    public ushort boneCount1;
    public ushort boneCount2;
    public ushort boneCount3;
    public ushort boneCount4;
    public ushort boneCount5;
    public ushort boneCount6;
    public ushort boneCount7;
    public ushort boneCountn1;
    public ushort padding1;
    public fixed byte unk3[11];
    public byte unkStruct2Count;
    public fixed byte unk3B[4];
    public uint unk4;
    public uint inputElementCount;
    public uint unk5;
    public uint unk1p0;
    public fixed float unk6[4];
    public fixed float unk7[4];
    public fixed float unk8[4];
    public fixed uint unk9[4];
    public fixed float unkA[4];
    public fixed uint unkB[4];
    public uint unkC;
    public ushort materialACount;
    public ushort materialBCount;
    public uint unkD;
    public uint unkE;
    public byte vertexBufferCount;
    public byte indiceBufferCount;
    public byte meshFlags;
    public byte submeshCount;
    public uint unkCount1;
    public ulong unkStruct2Ptr;
    public ulong materialBufferPtr;
    public ulong submeshBufferPtr;
    public ulong vertexBufferPtr;
    public ulong indiceBufferPtr;
    public ulong buffer4Ptr;
    public ulong buffer5Ptr;
    public ulong buffer6Ptr;
    public ulong unkStruct3Ptr;
    public ulong buffer8Ptr;
    public ulong buffer9Ptr;
    public ulong bufferAPtr;
    public ulong physicsBufferPtr;
    public ulong unkStruct1Ptr2;
    public ulong bufferDPtr;
    public ulong bufferEPtr;
    public ulong bufferFPtr;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public unsafe struct ModelSubmesh {
    public ulong effed;            //
    public uint unk1;              //
    public fixed float unk2[10];   //
    public uint vertexStart;       // vbOff
    public ushort indexStart;      // i0
    public ushort indexEnd;        // iN
    public ushort indiceCount;     //
    public ushort vertexCount;     //
    public ushort boneOffset;      // 
    public byte vertexBufferIndex; // vb
    public byte indexBufferIndex;  // fb
    public byte unk4;              // 
    public byte material;          // mat
    public byte lod;               // lod
    public byte unk5;              //
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public unsafe struct ModelVertexBuffer {
    public uint inputElementCount;
    public uint unk1;
    public byte strideStream1;
    public byte strideStream2;
    public byte vertexElements;
    public byte unk0;
    public uint unk2;
    public ulong effed1;
    public ulong effed2;
    public ulong vertexElementPtr;
    public ulong stream1Ptr;
    public ulong stream2Ptr;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct ModelIndiceBuffer {
    public uint indiceCount;
    public uint format;
    public ulong effed;
    public ulong stream1Ptr;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct ModelUnkStruct1 {
    public fixed uint unk1[20];
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct ModelAttachmentPoint {
    public Matrix4B matrix;
    public uint id;
    public ushort unk1;
    public byte unk2;
    public byte binary_size;
    public uint unk3;
    public ushort unk5;
    public ushort unk5b;
    public ulong unkBindataPtr;
    public fixed uint padding[2];
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public unsafe struct ModelUnkStruct3 {
    public ulong vec3Ptr;
    public ulong vec4Ptr;
    public ulong unk1Ptr;
    public ulong unk2Ptr;
    public fixed float unk1[3];
    public uint vec3Size;
    public uint vec4Size;
    public fixed float unk2[2];
    public fixed uint effed1[2];
    public fixed float unk3[12];
    public ushort effed2;
    public fixed byte unk4[18];
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct ModelIndice {
    public ushort v1;
    public ushort v2;
    public ushort v3;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct ModelUV {
    public Half u;
    public Half v;
  }
  
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct ModelVertexElement {
    public ModelVertexType type;
    public byte index;
    public ModelVertexFormat format;
    public byte stream;
    public ushort unk;
    public ushort offset;
  }

  public enum ModelVertexType : byte {
    POSITION    = 0x0,
    NORMAL      = 0x1,
    TANGENT     = 0x2,
    BINORMAL    = 0X3,
    BONE_INDEX  = 0x4,
    BONE_WEIGHT = 0x5,
    UV          = 0x9,
    SEQUENCE    = 0x10
  };

  public enum ModelVertexFormat : byte {
    NONE         = 0x0,
    SINGLE_3     = 0x2,
    HALF_2       = 0x4,
    UINT8_4      = 0x6,
    UINT8_UNORM4 = 0x8,
    UINT8_SNORM4 = 0x9,
    UINT32       = 0xC
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct ModelVertex {
    public float x;
    public float y;
    public float z;
  }
  
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public unsafe struct ModelBoneData {
    public ushort[] boneIndex;
    public float[] boneWeight;
  }
}
