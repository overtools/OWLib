using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
  public class EmoteItem : IInventorySTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct EmoteData {
      public OWRecord f014;
      public uint unk1;
      public float unk2;
      public ulong unk3;
      public uint unk4;
      public float unk5;
      public ulong unk6;
    }

    public ulong Key => 0xF71CD4D63A3838E4;
    public uint Id => 0xE533D614;
    public string Name => "Emote";

    private InventoryItemHeader header;
    public InventoryItemHeader Header => header;

    private EmoteData data;
    public EmoteData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<InventoryItemHeader>();
        data = reader.Read<EmoteData>();
      }
    }
  }
}
