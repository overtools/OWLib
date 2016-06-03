using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {

  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  public struct A301496F_Header {
    public ulong unk1;
    public ulong indicePtr;
    public ulong unk2;
    public ulong unk3;
    public ulong unk4;
    public ulong padding;
    public ulong unk5;
    public ulong unkDataPtr;
    public ulong unk6;
    public ulong materialDataPtr;
    public ulong unk7;
    public ulong unk8;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct A301496FMaterialDefinition {
    public ulong key;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct A301496FMaterialBind {
    public ulong key;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct A301496FMaterialData {
    public ulong key;
    public ulong value;
  }

  public struct A301496FMaterialDataContainer {
    public STUDDataPair<A301496FMaterialData> data;
    public STUDDataPair<A301496FMaterialBind>[] binds;
  }

  public class A301496F : STUDBlob {
    public static new uint id = 0xA301496F;

    private A301496F_Header header;
    private STUDDataPair<A301496FMaterialDefinition>[] materialTable;
    private STUDDataHeader[] markerData;
    private STUDDataHeader[] indiceData;
    private A301496FMaterialDataContainer[] materialDataParam;
    private STUDTableInstanceRecord instance;

    public A301496F_Header Header => header;
    public STUDDataPair<A301496FMaterialDefinition>[] MaterialTable => materialTable;
    public STUDDataHeader[] MarkerData => markerData;
    public STUDDataHeader[] IndiceData => indiceData;
    public A301496FMaterialDataContainer[] MaterialDataParam => materialDataParam;
    public STUDTableInstanceRecord Instance => instance;

    public new void Dump(TextWriter writer) {
      writer.WriteLine("Root instance...");
      OWLib.STUD.DumpInstance(writer, instance);
      writer.WriteLine("");

      writer.WriteLine("{0} materials...", materialTable.Length);
      for(int i = 0; i < materialTable.Length; ++i) {
        DumpSTUDHeader(writer, materialTable[i].header);
        writer.WriteLine("\tKey: {0}", materialTable[i].data.key);
        writer.WriteLine("");
      }

      writer.WriteLine("{0} markers...", markerData.Length);
      for(int i = 0; i < markerData.Length; ++i) {
        DumpSTUDHeader(writer, markerData[i]);
        writer.WriteLine("");
      }

      writer.WriteLine("{0} indices...", indiceData.Length);
      for(int i = 0; i < indiceData.Length; ++i) {
        DumpSTUDHeader(writer, indiceData[i]);
        writer.WriteLine("");
      }

      writer.WriteLine("{0} params...", materialDataParam.Length);
      for(int i = 0; i < materialDataParam.Length; ++i) {
        DumpSTUDHeader(writer, materialDataParam[i].data.header);
        writer.WriteLine("\tK: {0}", materialDataParam[i].data.data.key);
        writer.WriteLine("\tV: {0}", materialDataParam[i].data.data.value);
        writer.WriteLine("\t{0} binds...", materialDataParam[i].binds.Length);
        for(int j = 0; j < materialDataParam[i].binds.Length; ++j) {
          DumpSTUDHeader(writer, materialDataParam[i].binds[j].header, "\t");
          writer.WriteLine("\t\tKey: {0}", materialDataParam[i].binds[j].data.key);
          writer.WriteLine("");
        }
        writer.WriteLine("");
      }
    }

    public new void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<A301496F_Header>();
        STUDPointer ptr = reader.Read<STUDPointer>();
        input.Seek((long)ptr.offset, SeekOrigin.Begin);

        materialTable = new STUDDataPair<A301496FMaterialDefinition>[ptr.count];
        for(ulong i = 0; i < ptr.count; ++i) {
          materialTable[i] = new STUDDataPair<A301496FMaterialDefinition> {
            header = reader.Read<STUDDataHeader>(),
            data = reader.Read<A301496FMaterialDefinition>()
          };
        }

        if(header.indicePtr > 0) {
          input.Seek((long)header.indicePtr, SeekOrigin.Begin);
          ptr = reader.Read<STUDPointer>();
          input.Seek((long)ptr.offset, SeekOrigin.Begin);
          indiceData = new STUDDataHeader[ptr.count];
          for(ulong i = 0; i < ptr.count; ++i) {
            indiceData[i] = reader.Read<STUDDataHeader>();
          }
        } else {
          indiceData = new STUDDataHeader[0];
        }

        input.Seek((long)header.unkDataPtr, SeekOrigin.Begin);
        ptr = reader.Read<STUDPointer>();
        input.Seek((long)ptr.offset, SeekOrigin.Begin);
        markerData = new STUDDataHeader[ptr.count];
        for(ulong i = 0; i < ptr.count; ++i) {
          markerData[i] = reader.Read<STUDDataHeader>();
        }

        input.Seek((long)header.materialDataPtr, SeekOrigin.Begin);
        ptr = reader.Read<STUDPointer>();
        input.Seek((long)ptr.offset, SeekOrigin.Begin);
        materialDataParam = new A301496FMaterialDataContainer[ptr.count];
        for(ulong i = 0; i < ptr.count; ++i) {
          reader.ReadUInt64(); // ?
          materialDataParam[i] = new A301496FMaterialDataContainer {
            data = new STUDDataPair<A301496FMaterialData> {
              header = reader.Read<STUDDataHeader>(),
              data = reader.Read<A301496FMaterialData>()
            },
            binds = null
          };
        }

        input.Seek((long)(ptr.offset + ptr.count * 40), SeekOrigin.Begin);
        for(ulong i = 0; i < ptr.count; ++i) {
          STUDPointer ptr2 = reader.Read<STUDPointer>();
          input.Seek((long)ptr2.offset, SeekOrigin.Begin);
          materialDataParam[i].binds = new STUDDataPair<A301496FMaterialBind>[ptr2.count];
          for(ulong j = 0; j < ptr2.count; ++j) {
            materialDataParam[i].binds[j] = new STUDDataPair<A301496FMaterialBind> {
              header = reader.Read<STUDDataHeader>(),
              data = reader.Read<A301496FMaterialBind>()
            };
          }
        }

        ptr = reader.Read<STUDPointer>();
        input.Seek((long)ptr.offset, SeekOrigin.Begin);
        instance = reader.Read<STUDTableInstanceRecord>();
      }
    }
  }
}
