using System.Runtime.InteropServices;

namespace STULib.Types.Generic {
    public static class Version2 {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUHeader {
            public uint InstanceCount;
            public uint InstanceListOffset;
            public uint Unknown1; // Zero
            public uint Unknown2; // Same as InstanceVariableListOffset
            public uint InstanceVariableListCount;
            public uint InstanceVariableListOffset;
            public uint Unknown3; // Zero
            public uint Unknown4; // ?
            public uint DataOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceRecord {
            public uint InstanceChecksum;
            public uint UnknownChecksum;
            public int Index;
            public uint Size;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceVariableListInfo {
            public uint Count;
            public uint Offset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceVariableListEntry {
            public uint Checksum;
            public uint Size;
        }
    }
}
