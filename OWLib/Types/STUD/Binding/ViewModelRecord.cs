using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class ViewModelRecord : ISTUDInstance {
        public uint Id => 0xDAF7A654;
        public string Name => "Binding:ViewModel";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ViewModel {
            public STUDInstanceInfo instance;
            public OWRecord binding;
            public float unk1;
            public float unk2;
            public float unk3;
            public uint unk4;
            public uint unk5;
            public uint unk6;
        }

        private ViewModel data;
        public ViewModel Data => data;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                data = reader.Read<ViewModel>();
            }
        }
    }
}
