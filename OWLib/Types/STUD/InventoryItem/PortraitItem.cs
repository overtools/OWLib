using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class PortraitItem : IInventorySTUDInstance {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct PortraitItemData {
            public OWRecord portrait;
            public OWRecord portrait2;
            public uint tier;
            public ushort bracket;
            public ushort star;
        }

        public uint Id => 0x3ECCEB5D;
        public string Name => "Portrait";

        private InventoryItemHeader header;
        public InventoryItemHeader Header => header;

        private PortraitItemData data;
        public PortraitItemData Data => data;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<InventoryItemHeader>();
                data = reader.Read<PortraitItemData>();
            }
        }
    }
}
