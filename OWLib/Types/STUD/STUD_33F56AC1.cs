using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x33F56AC1Header {
    public ulong offsetTypes;
    public ulong zero1;
    public ulong offsetRarities;
    public ulong zero2;
    public ulong offsetItems;
    public ulong zero3;
  }
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x33F56AC1Group {
    public ulong zero1;
    public ulong offset;
    public ulong zero2;
    public ulong zero3;
  }


  public class x33F56AC1 : STUDBlob {
    public static new uint id = 0x33F56AC1;
    public static new string name = "Inventory Master";

    private x33F56AC1Header header;
    private STUDDataHeader[] achievables;
    private STUDDataHeader[][] defaults;
    private STUDDataHeader[][] items;

    public x33F56AC1Header Header => header;
    public STUDDataHeader[] Achievables => achievables;
    public STUDDataHeader[][] Defaults => defaults;
    public STUDDataHeader[][] Items => items;

    public new void Dump(TextWriter writer) {
      writer.WriteLine("{0} achievables", achievables.Length);
      for(int i = 0; i < achievables.Length; ++i) {
        DumpKey(writer, achievables[i].key, "\t");
      }

      writer.WriteLine("{0} default groups", defaults.Length);
      for(int i = 0; i < defaults.Length; ++i) {
        writer.WriteLine("\t{0} defaults", defaults[i].Length);
        for(int j = 0; j < defaults[i].Length; ++j) {
          DumpKey(writer, defaults[i][j].key, "\t\t");
        }
      }

      writer.WriteLine("{0} item groups", items.Length);
      for(int i = 0; i < items.Length; ++i) {
        writer.WriteLine("\t{0} items", items[i].Length);
        for(int j = 0; j < items[i].Length; ++j) {
          DumpKey(writer, items[i][j].key, "\t\t");
        }
      }
    }

    public new void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<x33F56AC1Header>();

        input.Position = (long)header.offsetTypes;
        STUDPointer ptr = reader.Read<STUDPointer>();
        input.Position = (long)ptr.offset;
        achievables = new STUDDataHeader[ptr.count];
        for(ulong i = 0; i < ptr.count; ++i) {
          achievables[i] = reader.Read<STUDDataHeader>();
        }

        input.Position = (long)header.offsetRarities;
        ptr = reader.Read<STUDPointer>();
        defaults = new STUDDataHeader[ptr.count][];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          x33F56AC1Group group = reader.Read<x33F56AC1Group>();
          long tmp = input.Position;
          input.Position = (long)group.offset;
          STUDPointer ptr2 = reader.Read<STUDPointer>();
          defaults[i] = new STUDDataHeader[ptr2.count];
          for(ulong j = 0; j < ptr2.count; ++j) {
            defaults[i][j] = reader.Read<STUDDataHeader>();
          }
        }

        input.Position = (long)header.offsetItems;
        ptr = reader.Read<STUDPointer>();
        items = new STUDDataHeader[ptr.count][];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          x33F56AC1Group group = reader.Read<x33F56AC1Group>();
          long tmp = input.Position;
          input.Position = (long)group.offset;
          STUDPointer ptr2 = reader.Read<STUDPointer>();
          items[i] = new STUDDataHeader[ptr2.count];
          for(ulong j = 0; j < ptr2.count; ++j) {
            items[i][j] = reader.Read<STUDDataHeader>();
          }
        }
      }
    }
  }
}
