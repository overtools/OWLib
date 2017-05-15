using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class Pose : ISTUDInstance {
        public uint Id => 0x34C4FFE6;
        public string Name => "Pose";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct PoseHeader {
            public STUDInstanceInfo instance;
            public ulong zero;
            public OWRecord animation1;
            public fixed float unk1[18];
            public ulong unk2;
            public OWRecord animation2;
            public fixed float unk3[18];
            public ulong unk4;
            public OWRecord animation3;
            public fixed float unk5[18];
            public OWRecord unk6;
            public fixed float unk7[2];
        }

        private PoseHeader header;
        public PoseHeader Header => header;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<PoseHeader>();
            }
        }
    }
}
