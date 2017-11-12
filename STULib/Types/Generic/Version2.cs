using System.Diagnostics;
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

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceRecord {
            public uint InstanceChecksum;
            public uint AssignFieldChecksum;
            public int AssignInstanceIndex;
            public uint InstanceSize;

            internal string DebuggerDisplay => $"{InstanceChecksum:X} ({InstanceSize} bytes){(AssignInstanceIndex != -1 ? $" (Embedded in instance {AssignInstanceIndex}:{AssignFieldChecksum:X})" : "")}";
        }

        [DebuggerDisplay("Count: {" + nameof(FieldCount) + "}")]
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceFieldList {
            public uint FieldCount;
            public uint ListOffset;
        }

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceArrayRef {
            public uint Checksum;
            public uint Size;

            internal string DebuggerDisplay => $"{Checksum:X} ({Size} bytes)";
        }

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInstanceField {
            public uint FieldChecksum;
            public uint FieldSize;

            internal string DebuggerDisplay => $"{FieldChecksum:X} ({FieldSize} bytes)";
        }

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUInlineInfo {
            public uint Size;
            public int FieldListIndex;

            internal string DebuggerDisplay => $"Size: {Size}, FieldListIndex: {FieldListIndex}";
        }

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
        [StructLayout(LayoutKind.Sequential, Pack = 4)]  // different struct for better naming
        public struct STUInlineArrayInfo {
            public uint Size;
            public int Count;

            internal string DebuggerDisplay => $"Size: {Size}, Count: {Count}";
        }

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUArrayInfo {
            public uint Count;
            public uint InstanceIndex;
            public uint Offset;
            public uint Unknown;

            internal string DebuggerDisplay =>
                $"Count: {Count}, Offset: {Offset}, InstanceIndex:{InstanceIndex}, Unknown:{Unknown}";
        }

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct STUEmbedArrayInfo {
            public int Count;
            public uint Unknown;
            public long Offset;

            internal string DebuggerDisplay =>
                $"Count: {Count}, Offset: {Offset}, Unknown:{Unknown}";
        }
    }
}
