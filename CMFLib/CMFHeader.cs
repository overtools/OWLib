using System.Runtime.InteropServices;

namespace CMFLib {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CMFHeader {
        public uint BuildVersion;
        public uint Unknown0;
        public uint Unknown1;
        public uint Unknown2;
        public uint DataCount;
        public uint Unknown3;
        public uint EntryCount;
        public uint Magic;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CMFHeader17 {
        public uint BuildVersion;
        public uint Unknown0;
        public uint DataCount;
        public uint Unknown1;
        public uint EntryCount;
        public uint Magic;

        public CMFHeader Upgrade() {
            return new CMFHeader {
                BuildVersion = BuildVersion,
                Unknown0 = Unknown0,
                DataCount = DataCount,
                EntryCount = EntryCount,
                Magic = Magic
            };
        }
    }
}