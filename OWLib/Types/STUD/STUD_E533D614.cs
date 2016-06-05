using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct E533D614Header {
    public ulong padding;
    public ulong f014Key;
    public uint unk1;
    public float unk2;
    public ulong unk3;
    public ulong f020Key;
    public ulong unk4;
  }

  public class E533D614 : STUDInventoryItemGeneric {
    public static new uint id = 0xE533D614;

    private E533D614Header header;

    public E533D614Header Header => header;

    public new void Dump(TextWriter writer) {
      base.Dump(writer);
      writer.WriteLine("014:");
      DumpKey(writer, header.f014Key, "\t");
      writer.WriteLine("unk1: {0}", header.unk1);
      writer.WriteLine("unk2: {0}", header.unk2);
      writer.WriteLine("unk3: {0}", header.unk3);
      writer.WriteLine("020:");
      DumpKey(writer, header.f020Key, "\t");
      writer.WriteLine("unk4: {0}", header.unk3);
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<E533D614Header>();
      }
    }
  }
}
