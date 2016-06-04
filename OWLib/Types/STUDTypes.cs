using System.Runtime.InteropServices;
using System.IO;
using System;

namespace OWLib.Types {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct STUDHeader {
    public uint magic;
    public uint version;
    public uint size;
    public uint unk1;
    public uint type;
    public uint instances;
    public uint unk2;
    public uint unk3;
  }
  
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct STUDTableInstanceRecord {
    public uint id;
    public uint flags;
    public ulong key;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  public struct STUDPointer {
    public ulong count;
    public ulong offset;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct STUDDataHeader {
    public ulong padding;
    public ulong key;
  }

  public partial class STUDBlob {
    public static uint id = 0x00000000;

    public void Read(Stream stream) {
    }

    public void DumpKey(TextWriter writer, ulong key, string prefix = "") {
      writer.WriteLine("{1}\tIndex: {0:X12}", APM.keyToIndexID(key), prefix);
      writer.WriteLine("{1}\tType: {0:X3}", APM.keyToTypeID(key), prefix);
    }

    public void DumpSTUDHeader(TextWriter writer, STUDDataHeader header, string prefix = "") {
      DumpKey(writer, header.key, prefix);
    }

    public void Dump(TextWriter writer) {
      writer.Write("Whoops!");
    }
  }
}
