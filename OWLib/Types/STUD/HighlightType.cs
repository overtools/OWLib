using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class HighlightType : ISTUDInstance {
        public uint Id => 0xC0368123;
        public string Name => "HighlightType";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HighlightTypeData {
            public STUDInstanceInfo instance;
            public OWRecord name;
            public ulong unk1;  // 0
            public ulong unk2;  // 0
            public ulong unk3;  // 4683743613527313613 if special, 4683743613530669056 if normal
            public ulong unk4;  // 1
            public ulong unk5;  // 80
            public ulong unk6;  // 4294967312
        }

        private HighlightTypeData data;
        public HighlightTypeData Data => data;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                data = reader.Read<HighlightTypeData>();
            }
        }
    }
}
