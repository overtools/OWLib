using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
  public class WeaponSkinItem : IInventorySTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct WeaponSkinData {
      public ulong unk1;
      public ulong unk2;
    }

    public ulong Key => 0x34C28F86080AAB76;
    public uint Id => 0x01609B4D;
    public string Name => "Weapon Skin";

    private InventoryItemHeader header;
    public InventoryItemHeader Header => header;

    private WeaponSkinData data;
    public WeaponSkinData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<InventoryItemHeader>();
        data = reader.Read<WeaponSkinData>();
      }
    }
  }
}
