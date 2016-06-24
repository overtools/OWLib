using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct OWStringHeader {
    public ulong offset;
    public uint size;
    public uint references;
  }
  
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct OWRecord {
    public ulong padding;
    public ulong key;
  }
  
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct ImageDefinitionHeader {
    public ulong offset1;
    public ulong offset2;
    public ulong textureOffset;
    public ulong offset3;
    public uint unk1;
    public ushort unk2;
    public ushort unk3;
    public byte textureCount;
    public byte offset3Count;
    public ushort unk4;
    public uint unk5;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct ImageLayer {
    public ulong key;
    public uint unk;
    public uint layer;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct Vec4 {
    public float x;
    public float y;
    public float z;
    public float w;
  }
  
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct MD5Hash {
    public fixed byte Value[16];
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct Matrix4B {
    public fixed float Value[16];
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct Matrix3x4B {
    public fixed float Value[12];
  }

  public delegate Stream LookupContentByKeyDelegate(ulong key);
  public delegate Stream LookupContentByHashDelegate(MD5Hash hash);

  public enum HeroType : uint {
    OFFENSIVE = 1,
    DEFENSIVE = 2,
    TANK = 3,
    SUPPORT = 4
  }
}
