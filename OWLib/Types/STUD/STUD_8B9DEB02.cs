using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x8B9DEB02Header {
    public ulong padding;
    public ulong f0A6Key;
  }

  public class x8B9DEB02 : STUDInventoryItemGeneric {
    public static new uint id = 0x8B9DEB02;

    private x8B9DEB02Header header;

    public x8B9DEB02Header Header => header;

    public new void Dump(TextWriter writer) {
      base.Dump(writer);
      writer.WriteLine("0A6:");
      DumpKey(writer, header.f0A6Key, "\t");
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<x8B9DEB02Header>();
      }
    }
  }
}
