using System.Runtime.InteropServices;

namespace STULib.Types.Generic {
    public static class Version1 {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUHeader {
            public uint Magic;
            public uint InstanceCount;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceRecord {
            public int Offset;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUArray {
            public long EntryCount;
            public long Offset;
        }
    }
}
