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
    }
}
