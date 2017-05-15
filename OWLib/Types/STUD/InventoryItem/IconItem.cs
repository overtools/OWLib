using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class IconItem : IInventorySTUDInstance {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct IconItemData {
            public ulong unk1;
            public OWRecord f00D;
            public OWRecord decal;
            public ulong unk2;
        }

        public uint Id => 0x8CDAA871;
        public string Name => "Icon";

        private InventoryItemHeader header;
        public InventoryItemHeader Header => header;

        private IconItemData data;
        public IconItemData Data => data;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<InventoryItemHeader>();
                data = reader.Read<IconItemData>();
            }
        }
    }
}
