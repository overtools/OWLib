using System.Runtime.InteropServices;

namespace STULib.Types {
    public static class Generic {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUHeader {
            public uint Magic;
            public uint Version;
            public ulong InstanceTableOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceRecord {
            public uint Offset;
            public uint Flags;
        }
        
        public class STUInstance {
            public uint InstanceChecksum;
            public uint NextInstanceOffset;
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

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUReference {
            public long Offset;
        }
    }
}
