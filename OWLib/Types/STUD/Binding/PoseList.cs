using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
  public class PoseList : ISTUDInstance {
    public ulong Key => 0x6377FB6907E088FA;
    public uint Id => 0xDD3FC67D;
    public string Name => "Binding:PoseList";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PoseListHeader {
      public STUDInstanceInfo instance;
      public ulong arrayOffset;
      public ulong unk1;
      public ulong animationOffset;
      public ulong unk2;
      public float unk3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PoseListEntry {
      public OWRecord animation;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PoseListAnimation {
      public OWRecord animation;
      public ulong unk1;
      public ulong unk2;
      public ulong unk3;
      public ulong unk4;
    }

    private PoseListHeader header;
    private PoseListEntry[] entries;
    private PoseListAnimation[] animations;
    public PoseListHeader Header => header;
    public PoseListEntry[] Entries => entries;
    public PoseListAnimation[] Animations => animations;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<PoseListHeader>();

        if(header.arrayOffset == 0) {
          entries = new PoseListEntry[0];
        } else {
          input.Position = (long)header.arrayOffset;
          STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
          entries = new PoseListEntry[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            entries[i] = reader.Read<PoseListEntry>();
          }
        }

        if(header.animationOffset == 0) {
          animations = new PoseListAnimation[0];
        } else {
          input.Position = (long)header.animationOffset;
          STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
          animations = new PoseListAnimation[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            animations[i] = reader.Read<PoseListAnimation>();
          }
        }
      }
    }
  }
}
