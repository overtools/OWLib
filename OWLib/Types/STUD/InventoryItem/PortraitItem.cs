using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
  public class PortraitItem : IInventorySTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PortraitItemData {
      public OWRecord portrait;
      public OWRecord portrait2;
      public uint unk1;
      public uint index;
      public uint unk2;
    }

    public ulong Key
    {
      get
      {
        return 0x7931552C0E26DA69;
      }
    }

    public string Name
    {
      get
      {
        return "Portrait";
      }
    }

    private InventoryItemHeader header;
    public InventoryItemHeader Header => header;

    private PortraitItemData data;
    public PortraitItemData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<InventoryItemHeader>();
        data = reader.Read<PortraitItemData>();
      }
    }
  }
}
