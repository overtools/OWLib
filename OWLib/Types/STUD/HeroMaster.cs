using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class HeroMaster : ISTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct HeroMasterHeader {
      public STUDInstanceInfo instance;
      public OWRecord encryption;
      public uint zero1;
      public uint id;
      public ulong version;
      public OWRecord binding;
      public OWRecord name;
      public OWRecord zero2;
      public ulong recordsOffset;
      public ulong zero3;
      public OWRecord virtualSpace;
      public ulong virtualOffset;
      public ulong zero4;
      public OWRecord child1;
      public OWRecord child2;
      public OWRecord child3;
      public OWRecord child4;
      public ulong child1Offset;
      public ulong zero5;
      public ulong child2Offset;
      public ulong zero6;
      public OWRecord texture1;
      public OWRecord texture2;
      public OWRecord texture3;
      public OWRecord texture4;
      public ulong child3Offset;
      public ulong zero7;
      public ulong bindsOffset;
      public ulong zero8;
      public ulong zero9;
      public ulong zeroA;
      public OWRecord itemMaster;
      public fixed ushort zeroB[35];
      public fixed ushort unk1[9];
      public uint index;
      public HeroType type;
      public uint unk2;
      public uint unk3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct HeroChild1 {
      public ulong zero1;
      public OWRecord record;
      public uint zero2;
      public float unk;
      public uint zero3;
      public uint zero4;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct HeroChild2 {
      public ulong zero1;
      public OWRecord record;
      public uint zero2;
      public float unk1;
      public uint zero3;
      public float unk2;
      public uint zero4;
      public uint zero5;
    }

    public ulong Key
    {
      get
      {
        return 0x0640DC92294CCF91;
      }
    }

    public string Name
    {
      get
      {
        return "Hero Master";
      }
    }

    private HeroMasterHeader header;
    public HeroMasterHeader Header => header;

    private OWRecord[] r075Records;
    private OWRecord[] virtualRecords;
    private OWRecord[] r09ERecords;
    public OWRecord[] Record075 => r075Records;
    public OWRecord[] RecordVirtual => virtualRecords;
    public OWRecord[] Record09E => r09ERecords;

    private HeroChild1[] child1;
    private HeroChild2[] child2;
    private HeroChild2[] child3;
    public HeroChild1[] Child1 => child1;
    public HeroChild2[] Child2 => child2;
    public HeroChild2[] Child3 => child3;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<HeroMasterHeader>();
        
        input.Position = (long)header.recordsOffset;
        STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
        r075Records = new OWRecord[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          r075Records[i] = reader.Read<OWRecord>();
        }

        input.Position = (long)header.virtualOffset;
        ptr = reader.Read<STUDArrayInfo>();
        virtualRecords = new OWRecord[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          virtualRecords[i] = reader.Read<OWRecord>();
        }

        input.Position = (long)header.bindsOffset;
        ptr = reader.Read<STUDArrayInfo>();
        r09ERecords = new OWRecord[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          r09ERecords[i] = reader.Read<OWRecord>();
        }

        input.Position = (long)header.child1Offset;
        ptr = reader.Read<STUDArrayInfo>();
        child1 = new HeroChild1[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          child1[i] = reader.Read<HeroChild1>();
        }

        input.Position = (long)header.child2Offset;
        ptr = reader.Read<STUDArrayInfo>();
        child2 = new HeroChild2[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          child2[i] = reader.Read<HeroChild2>();
        }

        input.Position = (long)header.child3Offset;
        ptr = reader.Read<STUDArrayInfo>();
        child3 = new HeroChild2[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          child3[i] = reader.Read<HeroChild2>();
        }
      }
    }
  }
}
