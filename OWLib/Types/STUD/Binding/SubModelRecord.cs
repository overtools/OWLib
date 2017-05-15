using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class SubModelRecord : ISTUDInstance {
        public uint Id => 0xEC23FFFD;
        public string Name => "Binding:SubModel";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SubModel {
            public STUDInstanceInfo instance;
            public OWRecord binding;
            public OWRecord unk;
            public long offset;
            public ulong unk1;
            public long offset2;
            public ulong unk2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SubModelEntry {
            public ulong zero1;
            public OWRecord binding;
            public OWRecord virt1;
            public OWRecord virt2;
            public ulong zero2;
        }

        private SubModel header;
        public SubModel Header => header;

        private SubModelEntry[] entries;
        public SubModelEntry[] Entries => entries;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<SubModel>();

                if (header.offset > 0) {
                    input.Position = header.offset;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    entries = new SubModelEntry[info.count];
                    input.Position = (long)info.offset;
                    for (ulong i = 0; i < info.count; ++i) {
                        entries[i] = reader.Read<SubModelEntry>();
                    }
                } else {
                    entries = new SubModelEntry[0];
                }
            }
        }
    }
}
