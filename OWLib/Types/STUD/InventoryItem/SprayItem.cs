using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
  public class SprayItem : IInventorySTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SprayItemData {
      public ulong unk1;
      public OWRecord f00D;
      public OWRecord decal;
      public ulong unk2;
    }
    
    public uint Id => 0x15720E8A;
    public string Name => "Spray";

    private InventoryItemHeader header;
    public InventoryItemHeader Header => header;

    private SprayItemData data;
    public SprayItemData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
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

    public ulong Key => 0x1EBE13D167E67948;
    public uint Id => 0x6DA011A1;
    public string Name => "Spray:Mipmap";
    
    private MipmapData data;
    public MipmapData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        data = reader.Read<MipmapData>();
      }
    }
  }
}
