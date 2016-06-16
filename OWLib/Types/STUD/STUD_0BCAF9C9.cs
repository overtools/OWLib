using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x0BCAF9C9Header {
    public ulong unk1;
  }

  public class x0BCAF9C9 : STUDInventoryItemGeneric {
    public static new uint id = 0x0BCAF9C9;
    public static new string name = "Credits";

    private x0BCAF9C9Header header;

    public x0BCAF9C9Header Header => header;

    public new void Dump(TextWriter writer) {
      base.Dump(writer);
      writer.WriteLine("unk1", header.unk1);
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<x0BCAF9C9Header>();
      }
    }
  }
}
