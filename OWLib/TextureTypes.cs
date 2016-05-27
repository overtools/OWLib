using System.Runtime.InteropServices;

namespace OWLib {
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct TextureHeader { // 004 files
    public byte unk1;
    public ushort palette_sz;
    public byte format;
    public uint dataOffset;
    public ushort width;
    public ushort height;
    public ushort dataSize;
    public ulong referenceKey;
    public ulong unk2;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct RawTextureheader {
    public uint unk1;      // refInfo
    public uint unk2;      // refInfo
    public uint imageSize;
    public uint unk3;      // bitDepth
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public unsafe struct DDSHeader {
    public uint magic;
    public uint size;
    public uint flags;
    public uint height;
    public uint width;
    public uint linearSize;
    public uint depth;
    public uint mipmapCount;
    public fixed uint reserved1[11]; // * 11
    public DDSPixelFormat format;
    public uint caps1;
    public uint caps2;
    public uint caps3;
    public uint caps4;
    public uint reserved2;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct DDSPixelFormat {
    public uint size;
    public uint flags;
    public uint fourCC;
    public uint bitCount;
    public uint redMask;
    public uint greenMask;
    public uint blueMask;
    public uint alphaMask;
  }

  public enum TextureType : uint {
    Linear  = 0,
    ATI1    = 826889281,
    ATI2    = 843666497,
    DXT1    = 827611204,
    DXT2    = 844388420,
    DXT3    = 861165636,
    DXT4    = 877942852,
    DXT5    = 894720068,
    RXGB    = 1111971922,
    Unknown = 0xFFFFFFFF
  }
}
