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
            [STUField(STUVersionOnly = new uint[] {1}, IgnoreVersion = new[] {0xc41B27A5})]
            private ulong Padding = ulong.MaxValue;

            [STUField(0xDEADBEEF, DummySize = 8)] // DUMMY
            private ulong Key;

            public static implicit operator long(STUGUID i) {
                return (long) i.Key;
            }

            public static implicit operator ulong(STUGUID i) {
                return i.Key;
            }

            public new string ToString() {
                return $"{GUID.LongKey(Key):X12}.{GUID.Type(Key):X3}" + (GUID.IsMangled(Key) ? " (Mangled)" : "");
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

            public void SetGUIDs(ulong[] GUIDs) {
                if (GUIDs?.Length > 0) {
                    Key = GUIDs[0];
                }
            }
        }
    }
}
