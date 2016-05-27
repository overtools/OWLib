using System.Runtime.InteropServices;

namespace OWLib.Types {
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public unsafe struct ModelHeader {
    fixed float boundingBox[16];
    uint blobPtr;
    uint unkStruct1Ptr;
    ulong eof;
    ulong unkFFFFPtr;
    fixed ulong boneMatrixPtr[4];
    ulong unkBoneU16;
    ulong boneIdPtr;
    ulong boneBindataPtr;
    ulong boneExPtr;
    ulong boneHierarchyPtr;
    uint magic;
    ushort bone1Count;
    ushort bone2Count;
    ushort unk1;
    ushort unk2;
    ushort bone3Count;
    ushort bone4Count;
    ushort bone5Count;
    ushort bone6Count;
    ushort boneCount;
    ushort padding1;
    fixed byte unk3[16];
    uint unk4;
    uint inputElementCount;
    uint unk5;
    uint unk1p0;
    fixed float unk6[4];
    fixed float unk7[4];
    fixed float unk8[4];
    fixed uint unk9[4];
    fixed float unkA[4];
    fixed uint unkB[4];
    uint unkC;
    ushort materialACount;
    ushort materialBCount;
    uint unkD;
    uint unkE;
    byte vertexBufferCount;
    byte indexBufferCount;
    byte meshFlags;
    byte submeshCount;
    uint unkCount1;
    ulong unkStruct2Ptr;
    ulong materialBufferPtr;
    ulong submeshBufferPtr;
    ulong vertexBufferPtr;
    ulong indiceBufferPtr;
    ulong buffer4Ptr;
    ulong buffer5Ptr;
    ulong buffer6Ptr;
    ulong unkStruct3Ptr;
    ulong buffer8Ptr;
    ulong buffer9Ptr;
    ulong bufferAPtr;
    ulong physicsBufferPtr;
    ulong unkStruct1Ptr2;
    ulong bufferDPtr;
    ulong bufferEPtr;
    ulong bufferFPtr;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public unsafe struct ModelSubmesh {
    ulong effed;
    ulong unk1;
    fixed float unk2[10];
    uint vertexStart;
    ushort indexStart;
    ushort indexEnd;
    ushort indiceCount;
    uint vertexCount;
    byte vertexBufferIndex;
    byte indexBufferIndex;
    byte unk3;
    byte material;
    byte lod;
    byte unk4;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public unsafe struct ModelVertexBuffer {
    uint inputElementCount;
    uint unk1;
    byte strideStream1;
    byte strideStream2;
    byte formatStream1;
    byte formatStream2;
    uint unk2;
    fixed ulong effed[2];
    ulong chainlinkPtr;
    ulong stream1Ptr;
    ulong stream2Ptr;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct ModelIndiceBuffer {
    uint indiceCount;
    uint format;
    ulong effed;
    ulong stream1Ptr;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct ModelUnkStruct1 {
    fixed uint unk1[20];
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct ModelUnkStruct2 {
    fixed float matrix[16];
    fixed float unk1[4];
    ulong unkBindataPtr;
    fixed uint padding[2];
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public unsafe struct ModelUnkStruct3 {
    ulong vec3Ptr;
    ulong vec4Ptr;
    ulong unk1Ptr;
    ulong unk2Ptr;
    fixed float unk1[3];
    uint vec3Size;
    uint vec4Size;
    fixed float unk2[2];
    fixed uint effed1[2];
    fixed float unk3[12];
    ushort effed2;
    fixed byte unk4[18];
  }
}
