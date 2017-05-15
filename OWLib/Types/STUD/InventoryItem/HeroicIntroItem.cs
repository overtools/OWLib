using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class HeroicIntroItem : IInventorySTUDInstance {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HeroicIntroData {
            public OWRecord f006;
            public ulong unk1;
            public ulong unk2;
        }

        public uint Id => 0x4EE84DC0;
        public string Name => "Heroic Intro";

        private InventoryItemHeader header;
        public InventoryItemHeader Header => header;

        private HeroicIntroData data;
        public HeroicIntroData Data => data;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<InventoryItemHeader>();
                data = reader.Read<HeroicIntroData>();
            }
        }
    }
}
