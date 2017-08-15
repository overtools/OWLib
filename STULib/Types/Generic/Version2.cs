using System.Runtime.InteropServices;

namespace STULib.Types.Generic {
    public static class Version2 {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUHeader {
            public uint InstanceCount;
            public uint InstanceListOffset;
            public uint EntryInstanceCount;
            public uint EntryInstanceListOffset;
            public uint InstanceFieldListCount;
            public uint InstanceFieldListOffset;
            public uint MetadataSize;
            public uint MetadataOffset;
            public uint Offset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceRecord {
            public uint InstanceChecksum;
            public uint AssignFieldChecksum;
            public int AssignInstanceIndex;
            public uint InstanceSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceFieldList {
            public uint FieldCount;
            public uint ListOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceField {
            public uint FieldChecksum;
            public uint FieldSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUNestedInfo {
            public uint Offset;
            public int FieldListIndex;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUArray {
            public uint Count;
            public uint InstanceIndex;
            public uint Offset;
            public uint Unknown;
        }
    }
}
