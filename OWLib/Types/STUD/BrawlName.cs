using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class BrawlName : ISTUDInstance {
        public uint Id => 0x4683D781;
        public string Name => "BrawlName";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct BrawlNameData {
            public STUDInstanceInfo instance;
            public OWRecord name;
            public ulong unk1; // 0
            public ulong unk2; // 1
            public ulong unk3; // 64
            public ulong unk4; // 4294967312
        }

        private BrawlNameData data;
        public BrawlNameData Data => data;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                data = reader.Read<BrawlNameData>();
            }
        }
    }
}
