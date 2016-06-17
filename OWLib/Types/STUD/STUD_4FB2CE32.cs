using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x4FB2CE32Header {
    public ulong unk1;
    public uint rarity;
    public uint unk2;
    public ulong unk3;
    public ulong unk4;
    public ulong unk5;
    public ulong unk6;
    public ulong unk7;
    public STUDDataHeader unk8;
    public ulong f0ADOffset;
    public ulong unk9;
    public ulong unkA;
    public ulong unkB;
    public ulong f058Key;
    public float unkC;
    public float unkD;
    public float unkE;
    public float unkF;
    public ulong unk10;
    public ulong count;
    public ulong indexOffset;
    public ulong referenceOffset;
  }
  
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x4FB2CE32Reference {
    public ulong key;
    public ulong offset;
  }
  
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct x4FB2CE32ReferenceData {
    public uint unk;
    public uint next;
    public ulong key;
  }

  public class x4FB2CE32 : STUDBlob {
    public static new uint id = 0x4FB2CE32;
    public static new string name = "Skin Master";

    private x4FB2CE32Header header;
    private uint[] indices;
    private x4FB2CE32Reference[] references;
    private x4FB2CE32ReferenceData[] data;
    private STUDDataHeader[] f0AD;
    
    public x4FB2CE32Header Header => header;
    public uint[] Indices => indices;
    public x4FB2CE32Reference[] References => references;
    public x4FB2CE32ReferenceData[] Data => data;
    public STUDDataHeader[] AD => f0AD;

    public new void Dump(TextWriter writer) {
      writer.WriteLine("unk1: {0}", header.unk1);
      writer.WriteLine("rarity: {0}", header.rarity);
      writer.WriteLine("unk2: {0}", header.unk2);
      writer.WriteLine("unk3: {0}", header.unk3);
      writer.WriteLine("unk4: {0}", header.unk4);
      writer.WriteLine("unk5: {0}", header.unk5);
      writer.WriteLine("unk6: {0}", header.unk6);
      writer.WriteLine("unk7: {0}", header.unk7);
      writer.WriteLine("unk8:");
      DumpKey(writer, header.unk8.key, "\t");
      writer.WriteLine("unk9: {0}", header.unk9);
      writer.WriteLine("unkA: {0}", header.unkA);
      writer.WriteLine("unkB: {0}", header.unkB);
      writer.WriteLine("f058:");
      DumpKey(writer, header.f058Key, "\t");
      writer.WriteLine("unkC: {0}", header.unkC);
      writer.WriteLine("unkD: {0}", header.unkD);
      writer.WriteLine("unkE: {0}", header.unkE);
      writer.WriteLine("unkF: {0}", header.unkF);
      writer.WriteLine("unk10: {0}", header.unk10);
      writer.WriteLine("{0} indices", header.count);
      for(ulong i = 0; i < header.count; ++i) {
        writer.WriteLine("{0}", indices[i]);
      }
      writer.WriteLine("{0} references", references.Length);
      for(int i = 0; i < references.Length; ++i) {
        writer.WriteLine("\tkey:");
        DumpKey(writer, references[i].key, "\t\t");
        writer.WriteLine("");
        writer.WriteLine("\t\tunk1: {0}", data[i].unk);
        writer.WriteLine("\t\tkey:");
        DumpKey(writer, data[i].key, "\t\t\t");
      }
      writer.WriteLine("{0} 0ADs", f0AD.Length);
      for(int i = 0; i < f0AD.Length; ++i) {
        DumpKey(writer, f0AD[i].key, "\t");
      }
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<x4FB2CE32Header>();

        input.Position = (long)header.indexOffset;
        indices = new uint[header.count];
        uint max = 0;
        for(ulong i = 0; i < header.count; ++i) {
          indices[i] = reader.ReadUInt32();
          max = Math.Max(max, indices[i]);
        }

        input.Position = (long)header.referenceOffset;
        references = new x4FB2CE32Reference[max];
        data = new x4FB2CE32ReferenceData[max];
        for(ulong i = 0; i < max; ++i) {
          references[i] = reader.Read<x4FB2CE32Reference>();
        }
        for(ulong i = 0; i < max; ++i) {
          input.Position = (long)references[i].offset;
          data[i] = reader.Read<x4FB2CE32ReferenceData>();
        }

        input.Position = (long)header.f0ADOffset;
        STUDPointer ptr = reader.Read<STUDPointer>();
        f0AD = new STUDDataHeader[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          f0AD[i] = reader.Read<STUDDataHeader>();
        }
      }
    }
  }
}
