using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
  public class Unknown5C4B5EF809855F8FItem : ISTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct UnknownItemData {
      public ulong unk1;
      public ulong unk2;
    }

    public ulong Key
    {
      get
      {
        return 0x5C4B5EF809855F8F;
      }
    }

    public string Name
    {
      get
      {
        return "Unknown5C4B5EF809855F8F";
      }
    }
    
    private InventoryItemHeader header;
    public InventoryItemHeader Header => header;

    private UnknownItemData data;
    public UnknownItemData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input)) {
        header = reader.Read<InventoryItemHeader>();
        data = reader.Read<UnknownItemData>();
      }
    }
  }
}
