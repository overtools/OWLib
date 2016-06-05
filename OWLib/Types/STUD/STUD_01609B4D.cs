using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x01609B4DHeader {
    public ulong unk1;
    public ulong unk2;
  }

  public class x01609B4D : STUDInventoryItemGeneric {
    public static new uint id = 0x01609B4D;

    private x01609B4DHeader header;

    public x01609B4DHeader Header => header;

    public new void Dump(TextWriter writer) {
      base.Dump(writer);
      writer.WriteLine("unk1: {0}", header.unk1);
      writer.WriteLine("unk2: {0}", header.unk2);
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<x01609B4DHeader>();
      }
    }
  }
}
