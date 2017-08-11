using System.Runtime.InteropServices;

namespace STULib.Types {
    public static class Generic {
        public class STUHeader {
            [STUElement(Verify = STU.MAGIC)]
            public uint Magic;
            public uint Version;
            public STUInstanceRecord[] InstanceTable;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceRecord {
            public uint Offset;
            public uint Flags;
        }
        
        public class STUInstance {
            public uint InstanceChecksum;
            public uint NextInstanceOffset;

            public override string ToString() {
                return STU.GetName(GetType());
            }
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
