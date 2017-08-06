using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class AbilityInfo : ISTUDInstance {
        public uint Id => 0x54024AFC;
        public string Name => "AbilityInfo";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct AbilityInfoData {
            public STUDInstanceInfo instance;
            public OWRecord name;  // 07C string
            public OWRecord description;  // 07C string
            public OWRecord unk1;  // 096 System Mapping
            public ulong unk2;  // needs verifing
            public uint unk3;  // needs verifing
            public uint unk4;  // needs verifing
            public OWRecord image;  // 004; display image
            public OWRecord video;  // 0B6 Bink Video; tutorial video
            // more data here, not sure what

            // data that I presume is stored here, but I haven't found yet: type (passive, ultimate, normal)

            // trial data:
            // public ulong unk5;
            // public uint unk6;
            // public uint unk7;
            // public uint unk8;
            // public OWRecord unk9;
        }

        private AbilityInfoData data;
        public AbilityInfoData Data => data;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                data = reader.Read<AbilityInfoData>();
            }
        }
    }
}
