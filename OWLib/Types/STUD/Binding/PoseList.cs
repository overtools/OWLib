using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class PoseList : ISTUDInstance {
        public uint Id => 0xDD3FC67D;
        public string Name => "Binding:PoseList";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct PoseListHeader {
            public STUDInstanceInfo instance;
            public OWRecord reference;
        }

        private PoseListHeader header;
        public PoseListHeader Header => header;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<PoseListHeader>();
            }
        }
    }
}
