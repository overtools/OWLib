using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x3ECC0B5DHeader {
    public ulong padding;
    public ulong textureKey;
    public ulong padding2;
    public ulong textureKey2;
    public uint unk1;
    public uint indice;
    public ulong unk2;
  }

  public class x3ECCEB5D : STUDInventoryItemGeneric {
    public static new uint id = 0x3ECCEB5D;
    public static new string name = "Portrait";

    private x3ECC0B5DHeader header;

    public x3ECC0B5DHeader Header => header;

    public new void Dump(TextWriter writer) {
      base.Dump(writer);
      writer.WriteLine("texture (1):");
      DumpKey(writer, header.textureKey, "\t");
      writer.WriteLine("texture (2):");
      DumpKey(writer, header.textureKey, "\t");
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
