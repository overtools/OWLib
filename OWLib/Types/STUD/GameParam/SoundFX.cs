using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.GameParam {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class SoundFX : ISTUDInstance {
        public uint Id => 0xF36D34BB;
        public string Name => "GameParameter:SoundFX";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Header {
            public STUDInstanceInfo instance;
            public ulong unk1;
            public OWRecord binding;
            public OWRecord binding2;
        }

        private Header param;
        public Header Param => param;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                param = reader.Read<Header>();
            }
        }
    }
}
