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
      public OWRecord zero_1300_1;
      public OWRecord virtualSpace1;
      public ulong virtualOffset;
      public ulong zero3;
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
      public ulong zero8;
      public ulong zero9;
      public ulong bindsOffset;
      public ulong zeroA;
      public ulong zeroA2;
      public ulong zeroA3;
      public OWRecord itemMaster;
      public fixed ushort zeroB[32];
      public ulong directiveOffset;
      public ulong zeroC;
      public ushort zeroD;
      public uint zeroF;
      public float unkf1;
      public ushort unk1;
      public float unkf2;
      public float unkf3;
      public float unkf4;
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

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct HeroDirective {
      public ulong zero1;
      public OWRecord textureReplacement;
      public OWRecord master;
      public ulong offsetSubs;
      public ulong zero2;
      public ulong zero3;
    }

    public ulong Key => 0x0640DC92294CCF91;
    public uint Id => 0x91E7843A;
    public string Name => "Hero Master";

    private HeroMasterHeader header;
    public HeroMasterHeader Header => header;
    
    private OWRecord[] virtualRecords;
    private OWRecord[] r09ERecords;
    public OWRecord[] RecordVirtual => virtualRecords;
    public OWRecord[] Record09E => r09ERecords;

    private HeroDirective[] directives;
    private OWRecord[][] directiveChild;
    public HeroDirective[] Directives => directives;
    public OWRecord[][] DirectiveChild => directiveChild;

    private HeroChild1[] child1;
    private HeroChild2[] child2;
    private HeroChild2[] child3;
    public HeroChild1[] Child1 => child1;
    public HeroChild2[] Child2 => child2;
    public HeroChild2[] Child3 => child3;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<HeroMasterHeader>();

        if((long)header.virtualOffset > 0) {
          input.Position = (long)header.virtualOffset;
          STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
          virtualRecords = new OWRecord[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            virtualRecords[i] = reader.Read<OWRecord>();
          }
        }

        if((long)header.bindsOffset > 0) {
          input.Position = (long)header.bindsOffset;
          STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
          r09ERecords = new OWRecord[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            r09ERecords[i] = reader.Read<OWRecord>();
          }
        }

        if((long)header.child1Offset > 0) {
          input.Position = (long)header.child1Offset;
          STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
          child1 = new HeroChild1[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            child1[i] = reader.Read<HeroChild1>();
          }
        }

        if((long)header.child2Offset > 0) {
          input.Position = (long)header.child2Offset;
          STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
          child2 = new HeroChild2[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            child2[i] = reader.Read<HeroChild2>();
          }
        }

        if((long)header.child3Offset > 0) {
          input.Position = (long)header.child3Offset;
          STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
          child3 = new HeroChild2[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            child3[i] = reader.Read<HeroChild2>();
          }
        }

        if((long)header.directiveOffset > 0) {
          input.Position = (long)header.directiveOffset;
          STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
          directives = new HeroDirective[ptr.count];
          directiveChild = new OWRecord[ptr.count][];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            directives[i] = reader.Read<HeroDirective>();
          }
          for(ulong i = 0; i < ptr.count; ++i) {
            if((long)directives[i].offsetSubs > 0) {
              STUDArrayInfo ptr2 = reader.Read<STUDArrayInfo>();
              directiveChild[i] = new OWRecord[ptr2.count];
              input.Position = (long)ptr2.offset;
              for(ulong j = 0; j < ptr2.count; ++j) {
                directiveChild[i][j] = reader.Read<OWRecord>();
              }
            }
          }
        }
      }
    }
  }
}
