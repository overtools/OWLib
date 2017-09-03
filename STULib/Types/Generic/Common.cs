using System.Runtime.InteropServices;
using OWLib;

namespace STULib.Types.Generic {
    public static class Common {
        public class STUInstance {
            // Version 1.0 prefix
            [STUField(STUVersionOnly = new uint[] { 1 })]
            public uint InstanceChecksum;

            [STUField(STUVersionOnly = new uint[] { 1 })]
            public uint NextInstanceOffset;

            // Version 2.0 prefix
            //[STUField(STUVersionOnly = new uint[] {2})]
            //public uint FieldListIndex;

            public override string ToString() {
                return ISTU.GetName(GetType());
            }
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
        [STUOverride(0xDEADBEEF, 8)] // DUMMY
        public class STUGUID : IDemangleable {
            [STUField(0x1, DummySize = 8, OnlyBuffer = true)] // DUMMY
            private ulong Padding = ulong.MaxValue;

            [STUField(0x2, DummySize = 8)] // DUMMY
            private ulong Key;

            public STUGUID() {
            }

            public STUGUID(ulong key) {
                Key = key;
            }

            public static implicit operator long(STUGUID i) {
                return (long)(ulong)i;
            }

            public static implicit operator ulong(STUGUID i) {
                return i?.Key ?? 0;
            }

            public new string ToString() {
                return $"{GUID.LongKey(Key):X12}.{GUID.Type(Key):X3}";
            }

            public ulong[] GetGUIDs() {
                return new[] {
                    Key
                };
            }

            public ulong[] GetGUIDXORs() {
                return new[] {
                    Padding
                };
            }

            // ReSharper disable once InconsistentNaming
            public void SetGUIDs(ulong[] GUIDs) {
                if (GUIDs?.Length > 0) {
                    Key = GUIDs[0];
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 8)] // DUMMY
        // ReSharper disable once InconsistentNaming
        public class ulonglong {
            [STUField(0x1, DummySize = 8)] // DUMMY
            public ulong A;
            [STUField(0x2, DummySize = 8)] // DUMMY
            public ulong B;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 8)] // DUMMY
        public class STUVec2 {
            [STUField(0x1, DummySize = 4)]
            public float X;

            [STUField(0x2, DummySize = 4)]
            public float Y;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 12)] // DUMMY
        public class STUVec3 : STUVec2 {
            [STUField(0x3, DummySize = 4)]
            public float Z;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [STUOverride(0xDEADBEEF, 16)] // DUMMY
        public class STUVec4 : STUVec3 {
            [STUField(0x4, DummySize = 4)]
            public float W;
        }
    }
}
