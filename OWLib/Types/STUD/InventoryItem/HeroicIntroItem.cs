using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
  public class HeroicIntroItem : IInventorySTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct HeroicIntroData {
      public OWRecord f006;
      public ulong unk1;
      public ulong unk2;
    }

    public ulong Key
    {
      get
      {
        return 0x0A4B72529459891E;
      }
    }

    public string Name
    {
      get
      {
        return "Heroic Intro";
      }
    }
    
    private InventoryItemHeader header;
    public InventoryItemHeader Header => header;

    private HeroicIntroData data;
    public HeroicIntroData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<InventoryItemHeader>();
        data = reader.Read<HeroicIntroData>();
      }
    }
  }
}
