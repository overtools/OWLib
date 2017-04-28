using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.InventoryItem {
    public class EmoteItem : IInventorySTUDInstance {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct EmoteData {
            public OWRecord f014;
            public OWRecord animation;
            public OWRecord unknown;
            public long offset;
            public long unknown1;
            public uint unknown2;
            public float duration;
            public float x;
            public float y;
            public float z;
            public float w;
        }

        public uint Id => 0xE533D614;
        public string Name => "Emote";

        private InventoryItemHeader header;
        public InventoryItemHeader Header => header;

        private EmoteData data;
        public EmoteData Data => data;

        private OWRecord[] subdata;
        public OWRecord[] Subdata => subdata;

        public void Read(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<InventoryItemHeader>();
                data = reader.Read<EmoteData>();
                subdata = new OWRecord[0];
                if (data.offset > 0) {
                    input.Position = data.offset;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    if (info.offset > 0) {
                        input.Position = (long)info.offset;
                        subdata = new OWRecord[info.count];
                        for (ulong i = 0; i < info.count; ++i) {
                            subdata[i] = reader.Read<OWRecord>();
                        }
                    }
                }
            }
        }
    }
}
