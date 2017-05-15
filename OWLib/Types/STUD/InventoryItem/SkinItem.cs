using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class SkinItem : IInventorySTUDInstance {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SkinData {
            public OWRecord skin;
        }

        public uint Id => 0x8B9DEB02;
        public string Name => "Skin";

        private InventoryItemHeader header;
        public InventoryItemHeader Header => header;

        private SkinData data;
        public SkinData Data => data;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<InventoryItemHeader>();
                data = reader.Read<SkinData>();
            }
        }
    }
}
