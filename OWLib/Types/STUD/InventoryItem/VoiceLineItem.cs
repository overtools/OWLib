using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
  public class VoiceLineItem : IInventorySTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VoiceLineData {
      public ulong unk1;
      public OWRecord f00D;
      public OWRecord decal;
      public OWRecord f00D_2;
      public ulong unk2;
    }

    public ulong Key
    {
      get
      {
        return 0x40BA18C08294158F;
      }
    }

    public string Name
    {
      get
      {
        return "Voice Line";
      }
    }

    private InventoryItemHeader header;
    public InventoryItemHeader Header => header;

    private VoiceLineData data;
    public VoiceLineData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<InventoryItemHeader>();
        data = reader.Read<VoiceLineData>();
      }
    }
  }
}
