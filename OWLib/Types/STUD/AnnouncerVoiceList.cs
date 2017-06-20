using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class AnnouncerVoiceList : ISTUDInstance {
        public uint Id => 0x248E334E;
        public string Name => "AnnouncerVoiceList";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct AnnouncerData {
            public STUDInstanceInfo instance;
            public long offsetLists;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct AnnouncerFXEntry {
            public long unknown;
            public OWRecord master;
            public long primary;
            public long zero1;
            public long secondary;
            public long zero2;
        }

        private AnnouncerData data;
        private AnnouncerFXEntry[] entries;
        private OWRecord[][] primary;
        private OWRecord[][] secondary;

        public AnnouncerData Data => data;
        public AnnouncerFXEntry[] Entries => entries;
        public OWRecord[][] Primary => primary;
        public OWRecord[][] Secondary => secondary;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                data = reader.Read<AnnouncerData>();

                if (data.offsetLists > 0) {
                    input.Position = data.offsetLists;
                    STUDArrayInfo array = reader.Read<STUDArrayInfo>();
                    entries = new AnnouncerFXEntry[array.count];
                    primary = new OWRecord[array.count][];
                    secondary = new OWRecord[array.count][];
                    input.Position = (long)array.offset;
                    for (ulong i = 0; i < array.count; ++i) {
                        entries[i] = reader.Read<AnnouncerFXEntry>();
                    }
                    
                    for (ulong i = 0; i < array.count; ++i) {
                        if (entries[i].primary > 0) {
                            input.Position = entries[i].primary ;
                            STUDArrayInfo innerArray = reader.Read<STUDArrayInfo>();
                            primary[i] = new OWRecord[innerArray.count];
                            input.Position = (long)innerArray.offset;
                            for (ulong j = 0; j < innerArray.count; ++j) {
                                primary[i][j] = reader.Read<OWRecord>();
                            }
                        } else {
                            primary[i] = new OWRecord[0];
                        }

                        if (entries[i].secondary > 0) {
                            input.Position = entries[i].secondary;
                            STUDArrayInfo innerArray = reader.Read<STUDArrayInfo>();
                            secondary[i] = new OWRecord[innerArray.count];
                            input.Position = (long)innerArray.offset;
                            for (ulong j = 0; j < innerArray.count; ++j) {
                                secondary[i][j] = reader.Read<OWRecord>();
                            }
                        } else {
                            secondary[i] = new OWRecord[0];
                        }
                    }
                } else {
                    entries = new AnnouncerFXEntry[0];
                    primary = new OWRecord[0][];
                    secondary = new OWRecord[0][];
                }
            }
        }
    }
}