using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x018667E2Header {
    public ulong padding;
    public ulong f0BFKey;
  }

  public class x018667E2 : STUDInventoryItemGeneric {
    public static new uint id = 0x018667E2;

    private x018667E2Header header;

    public x018667E2Header Header => header;

    public new void Dump(TextWriter writer) {
      base.Dump(writer);
      writer.WriteLine("0BF:");
      DumpKey(writer, header.f0BFKey, "\t");
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<x018667E2Header>();
      }
    }
  }
}
