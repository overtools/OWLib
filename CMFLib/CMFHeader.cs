using System.Runtime.InteropServices;

namespace CMFLib {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CMFHeader20 {  // pre 1.14
        public uint BuildVersion;
        public uint Unknown0;
        public uint DataCount;
        public uint Unknown1;
        public uint EntryCount;
        public uint Magic;

        public CMFHeaderCommon Upgrade() {
            return new CMFHeaderCommon {
                BuildVersion = BuildVersion,
                DataCount = DataCount,
                EntryCount = EntryCount,
                Magic = Magic
            };
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CMFHeader21 {  // 1.14-1.22
        public uint BuildVersion;
        public uint Unknown0;
        public uint Unknown1;
        public uint Unknown2;
        public uint DataCount;
        public uint Unknown3;
        public uint EntryCount;
        public uint Magic;
        
        public CMFHeaderCommon Upgrade() {
            return new CMFHeaderCommon {
                BuildVersion = BuildVersion,
                DataCount = DataCount,
                EntryCount = EntryCount,
                Magic = Magic
            };
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CMFHeader22 {  // 1.22+
        public uint BuildVersion; // 0
        public uint Unk01; // 4
        public uint Unk02; // 8
        public uint Unk03; // 12
        public uint Unk04; // 16
        public int DataCount; // 20
        public uint Unk05; // 24
        public int EntryCount; // 28
        // 0x16666D63 '\x16fmc' -> Not Encrypted
        // 0x636D6616 'cmf\x16' -> Encrypted
        public uint Magic; // 32
        
        public CMFHeaderCommon Upgrade() {
            return new CMFHeaderCommon {
                BuildVersion = BuildVersion,
                DataCount = (uint)DataCount,
                EntryCount = (uint)EntryCount,
                Magic = Magic
            };
        }
    }

    public class CMFHeaderCommon {
        public uint BuildVersion;
        public uint DataCount;
        public uint EntryCount;
        public uint Magic;
    }
}