using System.Runtime.InteropServices;

namespace OWLib.Types {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct DXBCHeader {
    public ulong offset;
    public ulong zero1;
    public uint timestamp;
    public ulong zero2;
    public uint uncompressedSize;
    public uint compressedSize;
    public uint flags;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct DXBCSecondaryHeader {
    public ulong offset;
    public ulong unk1;
    public ulong ffff;
    public ushort unk2;
    public ushort unk3;
    public uint zero1;
    public ulong reference;
    public uint uncompressedSize;
    public uint compressedSize;
  }
}
