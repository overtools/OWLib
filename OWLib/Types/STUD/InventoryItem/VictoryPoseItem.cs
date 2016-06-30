using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
  public class VictoryPoseItem : IInventorySTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VictoryPoseData {
      public OWRecord f0BF;
    }

    public ulong Key => 0x964150072700B0D0;
    public uint Id => 0x018667E2;
    public string Name => "Victory Pose";
    
    private InventoryItemHeader header;
    public InventoryItemHeader Header => header;

    private VictoryPoseData data;
    public VictoryPoseData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<InventoryItemHeader>();
        data = reader.Read<VictoryPoseData>();
      }
    }
  }
}
