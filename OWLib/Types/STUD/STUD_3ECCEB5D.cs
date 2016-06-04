using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x3ECC0B5DHeader {
    public ulong portraitKey;
    public ulong padding;
    public uint unk1;
    public uint indice;
    public uint unk2;
  }

  public class x3ECCEB5D : STUDInventoryItemGeneric {
    public static new uint id = 0x3ECCEB5D;

    private x3ECC0B5DHeader header;

    public x3ECC0B5DHeader Header => header;

    public new void Dump(TextWriter writer) {
      base.Dump(writer);
      writer.WriteLine("portrait:");
      DumpKey(writer, header.portraitKey, "\t");
      writer.WriteLine("unk1: {0}", header.unk1);
      writer.WriteLine("indice: {0}", header.indice);
      writer.WriteLine("unk2: {0}", header.unk2);
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<x3ECC0B5DHeader>();
      }
    }
  }
}
