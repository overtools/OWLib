using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class WeaponSkinItem : IInventorySTUDInstance {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct WeaponSkinData {
            public ulong index;
            public ulong unk2;
        }

        public uint Id => 0x01609B4D;
        public string Name => "Weapon Skin";

        private InventoryItemHeader header;
        public InventoryItemHeader Header => header;

        private WeaponSkinData data;
        public WeaponSkinData Data => data;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<InventoryItemHeader>();
                data = reader.Read<WeaponSkinData>();
            }
        }
    }
}
