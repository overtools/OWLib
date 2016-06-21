using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct STUDHeader {
    public uint magic;
    public uint version;
    public ulong instanceTableOffset;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct STUDInstanceRecord {
    public uint offset;
    public uint flags;
    public ulong key;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct STUDInstanceInfo {
    public uint localId;
    public uint nextInstance;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct STUDArrayInfo {
    public ulong count;
    public ulong offset;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct STUDReferenceArrayInfo {
    public ulong count;
    public ulong indiceOffset;
    public ulong referenceOffset;
  }

  public interface ISTUDInstance {
    string Name
    {
      get;
    }

    ulong Key
    {
      get;
    }

    void Read(Stream input);
  }

  public enum STUD_MANAGER_ERROR {
    E_SUCCESS = 0x00,
    E_ALREADY_ADDED = 0x01,
    E_FAULT = 0x02,
    E_FAULT_AT_ID = 0x03,
    E_FAULT_AT_NAME = 0x04,
    E_UNKNOWN_INSTANCE = 0x05,
    E_GENERIC = 0x06,
    E_DUPLICATE = 0x07
  }
}
