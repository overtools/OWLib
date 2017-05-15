using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class CreditItem : IInventorySTUDInstance {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct CreditData {
            public ulong value;
        }

        public uint Id => 0x0BCAF9C9;
        public string Name => "Credit";

        private InventoryItemHeader header;
        public InventoryItemHeader Header => header;

        private CreditData data;
        public CreditData Data => data;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<InventoryItemHeader>();
                data = reader.Read<CreditData>();
            }
        }
    }
}
