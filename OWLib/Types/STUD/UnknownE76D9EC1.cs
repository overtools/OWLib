using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class UnknownE76D9EC1 : ISTUDInstance {
        // There is currently only one of this file
        // References a bunch of 003 (Game Logic)
        public uint Id => 0xE76D9EC1;
        public string Name => "UnknownE76D9EC1";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct UnknownE76D9EC1Data {
            public STUDInstanceInfo instance;
            public ulong arrayInfoOffset;  // 48
            public ulong unk1;  // 0
            public ulong unk2;  // 0
            public ulong count;  // 7
            public ulong arrayDataOffset;  // 64
        }

        private UnknownE76D9EC1Data data;
        private OWRecord[] records;

        public UnknownE76D9EC1Data Data => data;
        public OWRecord[] Records => records;
        

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                data = reader.Read<UnknownE76D9EC1Data>();

                if (data.count > 0) {
                    input.Position = (long)data.arrayInfoOffset;
                    STUDArrayInfo array = reader.Read<STUDArrayInfo>();
                    records = new OWRecord[array.count];
                    input.Position = (long)array.offset;
                    for (ulong i = 0; i < array.count; ++i) {
                        records[i] = reader.Read<OWRecord>();
                    }
                } else {
                    records = new OWRecord[0];
                }
            }
        }
    }
}
