using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class SoundBindingReference : ISTUDInstance {
        public uint Id => 0x31B1E932;
        public string Name => "Sound Binding:Reference";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ReferenceData {
            public STUDInstanceInfo instance;
            public OWRecord sound;
            public uint Typus;
        }

        private ReferenceData reference;
        public ReferenceData Reference => reference;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                reference = reader.Read<ReferenceData>();
            }
        }
    }
}