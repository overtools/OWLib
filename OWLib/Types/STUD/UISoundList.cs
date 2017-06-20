using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class UISoundList : ISTUDInstance {
        public uint Id => 0x240DBF22;
        public string Name => "UISoundList";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SoundListData {
            public STUDInstanceInfo instance;
            public long offsetLists;
            long zero0;
            public long offsetVirtual;
            long zero1;
            public OWRecord unknown1;
            public OWRecord @virtual;
            public long unknown2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SoundListInfo {
            public long unknown0;
            public long offset;
            public long unknown1;
            public long type;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SoundListEntry {
            public long unknown;
            public OWRecord usage;
            public OWRecord sound;
            public OWRecord unknown2;
            public long unknown3;
            public long unknown4;
        }

        private SoundListData data;
        private SoundListInfo[] info;
        private SoundListEntry[][] entries;

        public SoundListData Data => data;
        public SoundListInfo[] Info => info;
        public SoundListEntry[][] Entries => entries;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                data = reader.Read<SoundListData>();

                if (data.offsetLists > 0) {
                    input.Position = data.offsetLists;
                    STUDArrayInfo array = reader.Read<STUDArrayInfo>();
                    info = new SoundListInfo[array.count];
                    entries = new SoundListEntry[array.count][];
                    input.Position = (long)array.offset;
                    for (ulong i = 0; i < array.count; ++i) {
                        info[i] = reader.Read<SoundListInfo>();
                    }
                    
                    for (ulong i = 0; i < array.count; ++i) {
                        if (info[i].offset > 0) {
                            input.Position = info[i].offset;
                            STUDArrayInfo innerArray = reader.Read<STUDArrayInfo>();
                            entries[i] = new SoundListEntry[innerArray.count];
                            input.Position = (long)innerArray.offset;
                            for (ulong j = 0; j < innerArray.count; ++j) {
                                entries[i][j] = reader.Read<SoundListEntry>();
                            }
                        } else {
                            entries[i] = new SoundListEntry[0];
                        }
                    }
                } else {
                    info = new SoundListInfo[0];
                    entries = new SoundListEntry[0][];
                }
            }
        }
    }
}