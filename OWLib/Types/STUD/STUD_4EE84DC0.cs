using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x4EE84DC0Header {
    public ulong padding;
    public ulong f006Key;
    public ulong unk1;
    public ulong unk2;
  }

  public class x4EE84DC0 : STUDInventoryItemGeneric {
    public static new uint id = 0x4EE84DC0;
    public static new string name = "Heroic Intro";

    private x4EE84DC0Header header;

    public x4EE84DC0Header Header => header;

    public new void Dump(TextWriter writer) {
      base.Dump(writer);
      writer.WriteLine("006:");
      DumpKey(writer, header.f006Key, "\t");
      writer.WriteLine("unk1: {0}", header.unk1);
      writer.WriteLine("unk2: {0}", header.unk2);
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<x4EE84DC0Header>();
      }
    }
  }
}
