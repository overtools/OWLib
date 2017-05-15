using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.GameParam {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class ChildGameParameterRecord : ISTUDInstance {
        public uint Id => 0x04E83493;
        public string Name => "GameParameter:Child";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ModelParam {
            public STUDInstanceInfo instance;
            public ulong unk1;
            public OWRecord binding;
            public OWRecord binding2;
        }

        private ModelParam param;
        public ModelParam Param => param;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                param = reader.Read<ModelParam>();
            }
        }
    }
}
