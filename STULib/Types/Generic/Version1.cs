using System.Runtime.InteropServices;

namespace STULib.Types.Generic {
    public static class Version1 {
        public class STUHeader {
            public uint Magic;
            public uint Version;
            public STUInstanceRecord[] InstanceTable;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceRecord {
            public uint Offset;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUArray {
            public long EntryCount;
            public long Offset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUReferenceArray {
            public long EntryCount;
            public long SizeOffset;
            public long DataOffset;
        }
    }
}
