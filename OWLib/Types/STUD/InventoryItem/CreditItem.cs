using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
  public class CreditItem : IInventorySTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CreditData {
      public ulong unk1;
    }

    public ulong Key => 0x5DA6FA7FF36B9C37;
    public uint Id => 0x0BCAF9C9;
    public string Name => "Credit";

    private InventoryItemHeader header;
    public InventoryItemHeader Header => header;

    private CreditData data;
    public CreditData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<InventoryItemHeader>();
        data = reader.Read<CreditData>();
      }
    }
  }
}
