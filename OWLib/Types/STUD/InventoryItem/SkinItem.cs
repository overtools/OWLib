using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
  public class SkinItem : IInventorySTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SkinData {
      public OWRecord skin;
    }

    public ulong Key
    {
      get
      {
        return 0x6A79CBB4BC5CDACC;
      }
    }

    public string Name
    {
      get
      {
        return "Skin";
      }
    }
    
    private InventoryItemHeader header;
    public InventoryItemHeader Header => header;

    private SkinData data;
    public SkinData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<InventoryItemHeader>();
        data = reader.Read<SkinData>();
      }
    }
  }
}
