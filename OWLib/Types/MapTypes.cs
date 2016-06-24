using System;
using System.Runtime.InteropServices;

namespace OWLib.Types {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapHeader {
    public uint recordCount;
    public uint sizes;
    public uint offset;
    public uint unk;
  }

  public enum MAP_MANAGER_ERROR {
    E_SUCCESS = 0x00,
    E_ALREADY_ADDED = 0x01,
    E_FAULT = 0x02,
    E_FAULT_AT_ID = 0x03,
    E_FAULT_AT_NAME = 0x04,
    E_UNKNOWN_TYPE = 0x05,
    E_GENERIC = 0x06,
    E_DUPLICATE = 0x07
  }
  
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct MapCommonHeader {
    public MD5Hash guid;
    public ushort mask;
    public ushort type;
    public uint size;
  }
}
