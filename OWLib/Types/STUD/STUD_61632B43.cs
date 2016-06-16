using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x61632B43Header {
    public ulong unk1;
    public ulong unk2;
  }

  public class x61632B43 : STUDInventoryItemGeneric {
    public static new uint id = 0x61632B43;
    public static new string name = "Unknown61632B43";

    private x61632B43Header header;

    public x61632B43Header Header => header;

    public new void Dump(TextWriter writer) {
      base.Dump(writer);
      writer.WriteLine("unk1", header.unk1);
      writer.WriteLine("unk2", header.unk2);
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<x61632B43Header>();
      }
    }
  }
}
