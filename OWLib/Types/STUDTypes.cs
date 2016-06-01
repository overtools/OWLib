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
    public uint id;
    public uint type;
  }

  public struct STUDDataPair<T> {
    public STUDDataHeader header;
    public T data;
  }

  public partial class STUDBlob {
    public static uint id = 0x00000000;

    public void Read(Stream stream) {
    }
    

    public void DumpSTUDHeader(TextWriter writer, STUDDataHeader header, string prefix = "") {
      writer.WriteLine("{1}\tID: {0}", header.id, prefix);
      writer.WriteLine("{1}\tType: {0}", header.type, prefix);
    }

    public void Dump(TextWriter writer) {
      writer.Write("Whoops!");
    }

    public void Dump(Stream output) {
      using(TextWriter writer = new StreamWriter(output)) {
        Dump(writer);
      }
    }

    public void Dump() {
      Dump(Console.Out);
    }
  }
}
