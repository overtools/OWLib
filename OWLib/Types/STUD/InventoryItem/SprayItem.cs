using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
  public class SprayItem : ISTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SprayItemData {
      public ulong unk1;
      public OWRecord f00D;
      public OWRecord decal;
      public ulong unk2;
    }

    public ulong Key
    {
      get
      {
        return 0xF19B41835220C528;
      }
    }

    public string Name
    {
      get
      {
        return "Spray";
      }
    }

    private InventoryItemHeader header;
    public InventoryItemHeader Header => header;

    private SprayItemData data;
    public SprayItemData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input)) {
        header = reader.Read<InventoryItemHeader>();
        data = reader.Read<SprayItemData>();
      }
    }
  }

  public class SprayMipmapItem : ISTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MipmapData {
      public STUDInstanceInfo instance;
      public OWRecord f00D;
      public OWRecord decal;
    }

    public ulong Key
    {
      get
      {
        return 0x1EBE13D167E67948;
      }
    }

    public string Name
    {
      get
      {
        return "Spray:Mipmap";
      }
    }
    
    private MipmapData data;
    public MipmapData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input)) {
        data = reader.Read<MipmapData>();
      }
    }
  }
}
