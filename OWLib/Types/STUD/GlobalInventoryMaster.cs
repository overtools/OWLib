using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class GlobalInventoryMaster : ISTUDInstance {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct InventoryMetadata {
            public STUDInstanceInfo instance;
            public ulong zero1;
            public long herowide;
            public long zero2;
            public long lootbox_info;
            public long zero3;
            public long event_box_info;
            public long zero4;
            public long lootbox_info_2;
            public long zero5;
            public long offsetarray;
            public long zero6;
            public long categories;
            public long zero7;
            public long lootbox_exclusive;
            public long zero8;
            public long end;
            public long zero9;
            public uint unknown1;
            public uint unknown2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct InventoryMasterGroup {
            public ulong zero1;
            public ulong offset;
            public ulong zero2;
            public ulong @event;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Bound {
            public OWRecord unknown;
            public OWRecord item;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Box {
            public OWRecord item;
            public long unknown;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct InventoryEntry {
            public long items;
            public ulong @event;
            public long unknown2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Category {
            public long zero;
            public long offset;
            public long unknown;
            public ulong @event;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Reward {
            public OWRecord item;
            public long unknown;
        }

        public uint Id => 0x33597A76;
        public string Name => "Global Inventory Master";

        private InventoryMetadata header;
        public InventoryMetadata Header => header;

        private OWRecord[] standardItems;
        private OWRecord[] lootboxInfo;
        private Bound[] eventLootboxInfo;
        private Box[] lootboxInfo2;
        private InventoryEntry[] generic;
        private OWRecord[][] genericItems;
        private Category[] categories;
        private OWRecord[][] categoryItems;
        private long[] exclusiveOffsets;
        private Reward[][] lootboxExclusive;

        public OWRecord[] StandardItems => standardItems;
        public OWRecord[] LootboxInfo => lootboxInfo;
        public Bound[] EventLootboxInfo => eventLootboxInfo;
        public Box[] LootboxInfo2 => lootboxInfo2;
        public InventoryEntry[] Generic => generic;
        public OWRecord[][] GenericItems => genericItems;
        public Category[] Categories => categories;
        public OWRecord[][] CategoryItems => categoryItems;
        public long[] ExclusiveOffsets => exclusiveOffsets;
        public Reward[][] LootboxExclusive => lootboxExclusive;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<InventoryMetadata>();
                
                if (header.herowide > 0) {
                    input.Position = header.herowide;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    standardItems = new OWRecord[info.count];
                    if (info.count > 0) {
                        input.Position = (long)info.offset;
                        for (ulong i = 0; i < info.count; ++i) {
                            standardItems[i] = reader.Read<OWRecord>();
                        }
                    }
                } else {
                    standardItems = new OWRecord[0] { };
                }

                if (header.lootbox_info > 0) {
                    input.Position = header.lootbox_info;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    lootboxInfo = new OWRecord[info.count];
                    if (info.count > 0) {
                        input.Position = (long)info.offset;
                        for (ulong i = 0; i < info.count; ++i) {
                            lootboxInfo[i] = reader.Read<OWRecord>();
                        }
                    }
                } else {
                    lootboxInfo = new OWRecord[0] { };
                }

                if (header.event_box_info > 0) {
                    input.Position = header.event_box_info;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    eventLootboxInfo = new Bound[info.count];
                    if (info.count > 0) {
                        input.Position = (long)info.offset;
                        for (ulong i = 0; i < info.count; ++i) {
                            eventLootboxInfo[i] = reader.Read<Bound>();
                        }
                    }
                } else {
                    eventLootboxInfo = new Bound[0] { };
                }

                if (header.lootbox_info_2 > 0) {
                    input.Position = header.lootbox_info_2;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    lootboxInfo2 = new Box[info.count];
                    if (info.count > 0) {
                        input.Position = (long)info.offset;
                        for (ulong i = 0; i < info.count; ++i) {
                            lootboxInfo2[i] = reader.Read<Box>();
                        }
                    }
                } else {
                    lootboxInfo2 = new Box[0] { };
                }

                if (header.offsetarray > 0) {
                    input.Position = header.offsetarray;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    generic = new InventoryEntry[info.count];
                    genericItems = new OWRecord[info.count][];
                    if (info.count > 0) {
                        input.Position = (long)info.offset;
                        for (ulong i = 0; i < info.count; ++i) {
                            generic[i] = reader.Read<InventoryEntry>();
                            long old = input.Position;
                            if (generic[i].items > 0) {
                                input.Position = generic[i].items;
                                STUDArrayInfo subinfo = reader.Read<STUDArrayInfo>();
                                if (subinfo.count > 0) {
                                    input.Position = (long)subinfo.offset;
                                    genericItems[i] = new OWRecord[subinfo.count];
                                    for (ulong j = 0; j < subinfo.count; ++j) {
                                        genericItems[i][j] = reader.Read<OWRecord>();
                                    }
                                } else {
                                    genericItems[i] = new OWRecord[0];
                                }
                            } else {
                                genericItems[i] = new OWRecord[0];
                            }
                            input.Position = old;
                        }
                    }
                } else {
                    generic = new InventoryEntry[0] { };
                    genericItems = new OWRecord[0][] { };
                }

                if (header.categories > 0) {
                    input.Position = header.categories;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    categories = new Category[info.count];
                    categoryItems = new OWRecord[info.count][];
                    if (info.count > 0) {
                        input.Position = (long)info.offset;
                        for (ulong i = 0; i < info.count; ++i) {
                            categories[i] = reader.Read<Category>();
                            long old = input.Position;
                            if (categories[i].offset > 0) {
                                input.Position = categories[i].offset;
                                STUDArrayInfo subinfo = reader.Read<STUDArrayInfo>();
                                if (subinfo.count > 0) {
                                    input.Position = (long)subinfo.offset;
                                    categoryItems[i] = new OWRecord[subinfo.count];
                                    for (ulong j = 0; j < subinfo.count; ++j) {
                                        categoryItems[i][j] = reader.Read<OWRecord>();
                                    }
                                } else {
                                    categoryItems[i] = new OWRecord[0];
                                }
                            } else {
                                categoryItems[i] = new OWRecord[0];
                            }
                            input.Position = old;
                        }
                    }
                } else {
                    categories = new Category[0] { };
                    categoryItems = new OWRecord[0][] { };
                }

                if (header.lootbox_exclusive > 0) {
                    input.Position = header.lootbox_exclusive;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    exclusiveOffsets = new long[info.count];
                    lootboxExclusive = new Reward[info.count][];
                    if (info.count > 0) {
                        input.Position = (long)info.offset;
                        for (ulong i = 0; i < info.count; ++i) {
                            exclusiveOffsets[i] = reader.ReadInt64();
                            long old = input.Position;
                            if (exclusiveOffsets[i] > 0) {
                                input.Position = exclusiveOffsets[i];
                                STUDArrayInfo subinfo = reader.Read<STUDArrayInfo>();
                                if (subinfo.count > 0) {
                                    input.Position = (long)subinfo.offset;
                                    lootboxExclusive[i] = new Reward[subinfo.count];
                                    for (ulong j = 0; j < subinfo.count; ++j) {
                                        lootboxExclusive[i][j] = reader.Read<Reward>();
                                    }
                                } else {
                                    lootboxExclusive[i] = new Reward[0];
                                }
                            } else {
                                lootboxExclusive[i] = new Reward[0];
                            }
                            input.Position = old;
                        }
                    }
                } else {
                    exclusiveOffsets = new long[0] { };
                    lootboxExclusive = new Reward[0][] { };
                }
            }
        }
    }
}
