using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
    public class BindingEffectReference : ISTUDInstance {
        public uint Id => 0xB709560A;

        public string Name => "Binding:BindingEffectReference";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ReferenceHeader {
            public STUDInstanceInfo instance;
            public long zero;
            public OWRecord key;
            public OWRecord effect;
        }

        private ReferenceHeader reference;
        public ReferenceHeader Reference => reference;

        public void Read(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                reference = reader.Read<ReferenceHeader>();
            }
        }
    }
}
