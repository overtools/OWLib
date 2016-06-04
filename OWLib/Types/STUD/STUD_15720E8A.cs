using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x15720E8AHeader {
    public ulong f00DKey;
    public ulong padding;
    public ulong f0A8Key;
    public uint unk1;
    public uint unk2;
    public uint unk3;
    public uint unk4;
    public ulong padding2;
    public ulong f00DKey2;
    public ulong padding3;
    public ulong f0A8Key2;
    public uint unk5;
    public uint unk6;
    public uint unk7;
    public uint unk8;
  }

  public class x15720E8A : STUDInventoryItemGeneric {
    public static new uint id = 0x15720E8A;

    private x15720E8AHeader header;

    public x15720E8AHeader Header => header;

    public new void Dump(TextWriter writer) {
      base.Dump(writer);
      writer.WriteLine("00D (1):");
      DumpKey(writer, header.f00DKey, "\t");
      writer.WriteLine("0A8 (1):");
      DumpKey(writer, header.f0A8Key, "\t");
      writer.WriteLine("unk1: {0}", header.unk1);
      writer.WriteLine("unk2: {0}", header.unk2);
      writer.WriteLine("unk3: {0}", header.unk3);
      writer.WriteLine("unk4: {0}", header.unk4);
      writer.WriteLine("00D (2):");
      DumpKey(writer, header.f00DKey2, "\t");
      writer.WriteLine("0A8 (2):");
      DumpKey(writer, header.f0A8Key2, "\t");
      writer.WriteLine("unk5: {0}", header.unk5);
      writer.WriteLine("unk6: {0}", header.unk6);
      writer.WriteLine("unk7: {0}", header.unk7);
      writer.WriteLine("unk8: {0}", header.unk8);
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<x15720E8AHeader>();
      }
    }
  }
}
