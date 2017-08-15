using System.Runtime.InteropServices;
using OWLib;

namespace STULib.Types.Generic {
    public static class Common {
        public class STUInstance {
            // Version 1.0 prefix
            [STUField(STUVersionOnly = new uint[] {1})]
            public uint InstanceChecksum;

            [STUField(STUVersionOnly = new uint[] {1})]
            public uint NextInstanceOffset;

            // Version 2.0 prefix
            //[STUField(STUVersionOnly = new uint[] {2})]
            //public uint FieldListIndex;

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

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 8)]
        public struct STUGUID {
            [STUField(STUVersionOnly = new uint[] {1})]
            private ulong Padding;

            [STUField(0xDEADBEEF)]
            private ulong Key;

            public static implicit operator long(STUGUID i) {
                return (long) i.Key;
            }

            public static implicit operator ulong(STUGUID i) {
                return i.Key;
            }

            public new string ToString() {
                return $"{GUID.LongKey(Key):X12}.{GUID.Type(Key):X3}";
            }
        }
    }
}
