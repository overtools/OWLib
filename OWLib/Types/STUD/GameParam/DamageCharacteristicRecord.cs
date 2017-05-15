using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.GameParam {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class DamageCharacteristicRecord : ISTUDInstance {
        public uint Id => 0x3AE7427E;
        public string Name => "GameParameter:DamageCharacteristic";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct DamageCharacteristic {
            public STUDInstanceInfo instance;
            public ulong unk1;
            public ulong value;
            public ulong unk2;
        }

        private DamageCharacteristic characteristic;
        public DamageCharacteristic Characteristic => characteristic;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                characteristic = reader.Read<DamageCharacteristic>();
            }
        }
    }
}
