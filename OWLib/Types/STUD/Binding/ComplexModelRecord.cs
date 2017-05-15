using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class ComplexModelRecord : ISTUDInstance {
        public uint Id => 0xBC1233E0;
        public string Name => "Binding:ComplexModel";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ComplexModel {
            public STUDInstanceInfo instance;
            public OWRecord animationList;
            public OWRecord secondaryAnimationList;
            public OWRecord tetriaryAnimationList;
            public OWRecord model;
            public OWRecord material;
            public ulong unk1;
            public ulong unk2;
            public ulong unk3;
            public float unk4;
            public uint unk5;
            public byte param1;
            public byte param2;
            public byte param3;
            public byte param4;
            public byte param5;
            public byte param6;
            public byte param7;
            public byte param8;
            public byte param9;
            public byte param10;
            public ushort unk6;
            public ushort unk7;
            public ushort unk8;
            public ushort unk9;
            public ushort unkA;
            public ushort unkB;
            public ushort unkC;
        }

        private ComplexModel data;
        public ComplexModel Data => data;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                data = reader.Read<ComplexModel>();
            }
        }
    }
}
