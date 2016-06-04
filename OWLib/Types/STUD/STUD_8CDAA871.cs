using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x8CDAA871Header {
    public ulong f00DKey;
    public ulong padding;
    public ulong f0A8Key;
    public uint unk1;
  }

  public class x8CDAA871 : STUDInventoryItemGeneric {
    public static new uint id = 0x8CDAA871;

    private x8CDAA871Header header;

    public x8CDAA871Header Header => header;

    public new void Dump(TextWriter writer) {
      base.Dump(writer);
      writer.WriteLine("00D:");
      DumpKey(writer, header.f00DKey, "\t");
      writer.WriteLine("0A8:");
      DumpKey(writer, header.f0A8Key, "\t");
      writer.WriteLine("unk1: {0}", header.unk1);
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<x8CDAA871Header>();
      }
    }
  }
}
