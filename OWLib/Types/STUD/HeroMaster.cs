using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class HeroMaster : ISTUDInstance {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct HeroMasterHeader {
            public STUDInstanceInfo instance;
            public OWRecord encryption;
            public uint zero1;
            public uint id;
            public ulong version;
            public OWRecord binding;
            public OWRecord name;
            public ulong offset__;
            public ulong unknwn;
            public OWRecord unk1;
            public ulong virtualOffset;
            public ulong zero3;
            public OWRecord virtualSpace1;
            public OWRecord child1;
            public OWRecord child2;
            public OWRecord child3;
            public OWRecord child4;
            public ulong child1Offset;
            public ulong zero4;
            public ulong child2Offset;
            public ulong zero5;
            public OWRecord texture1;
            public OWRecord texture2;
            public OWRecord texture3;
            public OWRecord texture4;
            public ulong child3Offset;
            public ulong zero6;
            public ulong zero7;
            public ulong zero8;
            public ulong bindsOffset;
            public ulong zero9;
            public ulong zero10;
            public ulong zero11;
            public OWRecord itemMaster;
            public fixed ushort zero12[40];
            public ulong directiveOffset;
            public ulong unkn;
            public float x;
            public float y;
            public float z;
            public float w;
            public uint id2;
            public uint index;
            public uint type;
            public uint subtype;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HeroChild1 {
            public ulong zero1;
            public OWRecord record;
            public uint zero2;
            public float unk;
            public uint zero3;
            public uint zero4;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HeroChild2 {
            public ulong zero1;
            public OWRecord record;
            public uint zero2;
            public float unk1;
            public uint zero3;
            public float unk2;
            public uint zero4;
            public uint zero5;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HeroDirective {
            public ulong zero1;
            public OWRecord textureReplacement;
            public OWRecord master;
            public ulong offsetSubs;
            public ulong zero2;
            public ulong zero3;
        }

        public uint Id => 0x91E7843A;
        public string Name => "Hero Master";

        private HeroMasterHeader header;
        public HeroMasterHeader Header => header;

        private OWRecord[] virtualRecords;
        private OWRecord[] r09ERecords;
        public OWRecord[] RecordVirtual => virtualRecords;
        public OWRecord[] Record09E => r09ERecords;

        private HeroDirective[] directives;
        private OWRecord[][] directiveChild;
        public HeroDirective[] Directives => directives;
        public OWRecord[][] DirectiveChild => directiveChild;

        private HeroChild1[] child1;
        private HeroChild2[] child2;
        private HeroChild2[] child3;
        public HeroChild1[] Child1 => child1;
        public HeroChild2[] Child2 => child2;
        public HeroChild2[] Child3 => child3;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<HeroMasterHeader>();
                long seekpos = input.Position;
#if OUTPUT_STUDHEROMASTER
                input.Seek(0, SeekOrigin.Begin);
                string outFilename = string.Format("./STUDs/HeroMaster/{0:X8}_{1}.stud", (0x00000000FFFFFFFF & header.name.key), header.id.ToString());
                string putPathname = outFilename.Substring(0, outFilename.LastIndexOf('/'));
                Directory.CreateDirectory(putPathname);
                Stream OutWriter = File.Create(outFilename);
                input.CopyTo(OutWriter);
                OutWriter.Close();
                input.Seek(seekpos, SeekOrigin.Begin);
#endif
                //Console.Out.WriteLine("Name: {8:X8}, ID: {0}, x: {1}, y: {2}, z: {3}, w: {4}, index: {5}, type: {6}, subtype: {7}, ItemKey: {9:X16}", header.id, header.x, header.y, header.z, header.w, header.index, header.type, header.subtype, (0x00000000FFFFFFFF & header.name.key), header.itemMaster.key);                
                if ((long)header.virtualOffset > 0) {
                    input.Position = (long)header.virtualOffset;
                    STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
                    virtualRecords = new OWRecord[ptr.count];
                    input.Position = (long)ptr.offset;
                    for (ulong i = 0; i < ptr.count; ++i) {
                        virtualRecords[i] = reader.Read<OWRecord>();
                    }
                } else {
                    virtualRecords = new OWRecord[0] { };
                }

                if ((long)header.bindsOffset > 0) {
                    input.Position = (long)header.bindsOffset;
                    STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
                    r09ERecords = new OWRecord[ptr.count];
                    input.Position = (long)ptr.offset;
                    for (ulong i = 0; i < ptr.count; ++i) {
                        r09ERecords[i] = reader.Read<OWRecord>();
                    }
                } else {
                    r09ERecords = new OWRecord[0] { };
                }

                if ((long)header.child1Offset > 0) {
                    input.Position = (long)header.child1Offset;
                    STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
                    child1 = new HeroChild1[ptr.count];
                    input.Position = (long)ptr.offset;
                    for (ulong i = 0; i < ptr.count; ++i) {
                        child1[i] = reader.Read<HeroChild1>();
                    }
                } else {
                    child1 = new HeroChild1[0] { };
                }

                if ((long)header.child2Offset > 0) {
                    input.Position = (long)header.child2Offset;
                    STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
                    child2 = new HeroChild2[ptr.count];
                    input.Position = (long)ptr.offset;
                    for (ulong i = 0; i < ptr.count; ++i) {
                        child2[i] = reader.Read<HeroChild2>();
                    }
                } else {
                    child2 = new HeroChild2[0] { };
                }

                if ((long)header.child3Offset > 0) {
                    input.Position = (long)header.child3Offset;
                    STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
                    child3 = new HeroChild2[ptr.count];
                    input.Position = (long)ptr.offset;
                    for (ulong i = 0; i < ptr.count; ++i) {
                        child3[i] = reader.Read<HeroChild2>();
                    }
                } else {
                    child3 = new HeroChild2[0] { };
                }

                if ((long)header.directiveOffset > 0) {
                    input.Position = (long)header.directiveOffset;
                    STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
                    directives = new HeroDirective[ptr.count];
                    directiveChild = new OWRecord[ptr.count][];
                    input.Position = (long)ptr.offset;
                    for (ulong i = 0; i < ptr.count; ++i) {
                        directives[i] = reader.Read<HeroDirective>();
                    }
                    for (ulong i = 0; i < ptr.count; ++i) {
                        if ((long)directives[i].offsetSubs > 0) {
                            STUDArrayInfo ptr2 = reader.Read<STUDArrayInfo>();
                            directiveChild[i] = new OWRecord[ptr2.count];
                            input.Position = (long)ptr2.offset;
                            for (ulong j = 0; j < ptr2.count; ++j) {
                                directiveChild[i][j] = reader.Read<OWRecord>();
                            }
                        } else {
                            directiveChild[i] = new OWRecord[0] { };
                        }
                    }
                } else {
                    directives = new HeroDirective[0] { };
                    directiveChild = new OWRecord[0][] { };
                }
            }
        }
    }
}
