using System.Runtime.InteropServices;

namespace STULib.Types.Generic {
    public static class Common {
        public class STUInstance {
            // Version 1.0 prefix
            [STUElement(STUVersionOnly = 1)]
            public uint InstanceChecksum;
            [STUElement(STUVersionOnly = 1)]
            public uint NextInstanceOffset;

            // Version 2.0 prefix
            [STUElement(STUVersionOnly = 2)]
            public uint FieldListIndex;

            public override string ToString() {
                return ISTU.GetName(GetType());
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

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUString {
            public uint Size;
            public uint Checksum;
            public long Offset;
        }
    }
}
