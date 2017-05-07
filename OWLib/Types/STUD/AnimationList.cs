using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class AnimationListInfo : ISTUDInstance {
    public uint Id => 0x73A19293;
    public string Name => "AnimationList:Info";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct AnimationListHeader {
      public STUDInstanceInfo instance;
      public ulong unk1_111;
      public ulong unk2_111;
      public ulong unk3_111;
      public ulong unk4_111;
      public ulong unk5_111;
      public OWRecord f015_1;
      public OWRecord f015_2;
      public OWRecord f015_3;
      public OWRecord f015_4;
      public OWRecord f015_5;
      public OWRecord f015_6;
      public OWRecord f015_7;
      public OWRecord f015_8;
      public OWRecord f015_9;
      public OWRecord f015_A;
      public OWRecord f015_B;
      public OWRecord f015_C;
      public ulong unk1;
      public ulong unk2;
      public ulong unk3;
      public ulong unk4;
      public ulong unk5;
      public ulong unk6;
      public ulong offset1;
      public ulong unk7;
      public ulong arrayOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AnimationListEntry {
      public OWRecord unk1;
      public float unk2;
      public float unk3;
      public ulong unk4;
      public ulong unk5;
      public ulong unk6;
      public OWRecord secondary;
      public OWRecord unk7;
      public ulong unk8;
      public ulong unk9;
      public ulong unkA;
      public ulong unkB;
      public ulong unkC;
      public ulong unkD;
    }

    private AnimationListHeader header;
    private AnimationListEntry[] entries;
    public AnimationListHeader Header => header;
    public AnimationListEntry[] Entries => entries;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<AnimationListHeader>();
        if(header.arrayOffset == 0) {
          entries = new AnimationListEntry[0];
          return;
        }
        input.Position = (long)header.arrayOffset;
        STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
        entries = new AnimationListEntry[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          entries[i] = reader.Read<AnimationListEntry>();
        }
      }
    }
  }

  public class AnimationList : ISTUDInstance {
    public ulong Key => 0x3ADB50778D44DF19;
    public uint Id => 0x7AE04211;
    public string Name => "AnimationList:List";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct AnimationListHeader {
      public STUDInstanceInfo instance;
      public ulong floatOffset;
      public fixed byte unk1[40];
      public ulong unk2Offset;
      public ulong unk3;
      public ulong arrayOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AnimationListEntry {
      public ulong unk1;
      public ulong unk2;
      public OWRecord animation;
    }

    private AnimationListHeader header;
    private AnimationListEntry[] entries;
    public AnimationListHeader Header => header;
    public AnimationListEntry[] Entries => entries;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<AnimationListHeader>();
        if(header.arrayOffset == 0) {
          entries = new AnimationListEntry[0];
          return;
        }
        input.Position = (long)header.arrayOffset;
        STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
        entries = new AnimationListEntry[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          entries[i] = reader.Read<AnimationListEntry>();
        }
      }
    }
  }

  public class AnimationListReference : ISTUDInstance {
    public ulong Key => 0xDA47E175439DCA09;
    public uint Id => 0xF2B831A7;
    public string Name => "AnimationList:Reference";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct Reference {
      public STUDInstanceInfo instance;
      public ulong unk1;
      public ulong unk2;
      public ulong unk3;
      public ulong unk4;
      public float unk5;
      public float unk6;
      public ulong unk7;
      public ulong unk8;
      public ulong unk9;
      public ulong unkA;
      public ulong unkB;
      public ulong unkC;
      public OWRecord unkD;
      public ulong unkE;
      public ulong unkF;
      public OWRecord animation;
      public ulong unk10;
      public ulong unk11;
      public OWRecord unk12;
      public ulong unk13;
      public ulong unk14;
      public OWRecord unk15;
      public ulong unk16;
      public ulong unk17;
      public OWRecord unk18;
      public ulong unk19;
      public ulong unk1A;
    }

    private Reference header;
    public Reference Header => header;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<Reference>();
      }
    }
  }
}
