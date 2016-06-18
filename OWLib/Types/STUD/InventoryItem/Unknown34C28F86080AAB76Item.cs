using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
  public class Unknown34C28F86080AAB76Item : IInventorySTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct UnknownItemData {
      public ulong unk1;
      public ulong unk2;
    }

    public ulong Key
    {
      get
      {
        return 0x34C28F86080AAB76;
      }
    }

    public string Name
    {
      get
      {
        return "Unknown34C28F86080AAB76";
      }
    }

    private InventoryItemHeader header;
    public InventoryItemHeader Header => header;

    private UnknownItemData data;
    public UnknownItemData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<InventoryItemHeader>();
        data = reader.Read<UnknownItemData>();
      }
    }
  }
}
