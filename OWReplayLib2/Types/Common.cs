using System.Runtime.InteropServices;
using OWLib;

namespace OWReplayLib2.Types {
    public static class Common {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct FileReference {
            public ulong Key;

            public override string ToString() {
                return $"{GUID.LongKey(Key):X12}.{GUID.Type(Key):X3}";
            }
        }
    }
}