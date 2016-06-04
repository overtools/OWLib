using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x090B30ABHeader {
    public ulong unk1;
    public ulong padding;
    public ulong f00DFile;
    public ulong padding2;
    public ulong f0A8File;
    public ulong padding3;
    public ulong f00DFile2;
    public ulong unk2;
  }

  public class x090B30AB : STUDInventoryItemGeneric {
    public static new uint id = 0x090B30AB;

    private x090B30ABHeader header;

    public x090B30ABHeader Header => header;

    public new void Dump(TextWriter writer) {
      base.Dump(writer);
      writer.WriteLine("unk1: {0}", header.unk1);
      writer.WriteLine("00D (1):");
      DumpKey(writer, header.f00DFile, "\t");
      writer.WriteLine("0A8:");
      DumpKey(writer, header.f0A8File, "\t");
      writer.WriteLine("00D (2):");
      DumpKey(writer, header.f00DFile2, "\t");
      writer.WriteLine("unk2: {0}", header.unk2);
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<x090B30ABHeader>();
      }
    }
  }
}
