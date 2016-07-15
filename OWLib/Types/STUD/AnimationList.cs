using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
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
}
