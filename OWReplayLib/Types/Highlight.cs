using System;
using System.Runtime.InteropServices;
using OWLib.Types;
using static OWReplayLib.Types.Common;

namespace OWReplayLib.Types {
    // https://github.com/willkirkby/overwatch-highlights/blob/master/OverwatchHighlights/Highlight.cs
    public static class Highlight {
        public const uint MAGIC_CONSTANT = 0x036C6870; // phl3, Player HighLight v3?

        [Flags]
        public enum HighlightFlags {
            Recent = 0x01,
            Manual = 0x02,
            Old = 0x04,
            Exported = 0x08
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HighlightHeader {
            public uint Magic;
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 0x20)]
            public byte[] Checksum;
            public uint DataLength;
            public ulonglong Unknown1;
            public ulonglong Unknown2;
            public uint ReplayVersion;
            public uint BuildVersion;
            public uint PlayerId;
            public uint Unknown3;
            public HighlightFlags Flags;
            public ulong MapDataKey;
            public ulong C5Key;
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayMarshaler<HighlightInfo, int>))]
            public HighlightInfo[] Info;
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayMarshaler<HeroInfo, int>))]
            public HeroInfo[] HeroInfo;
            public ulong Unknown4;
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayMarshaler<HighlightFilter, int>))]
            public HighlightFilter[] Filters;
            public int ReplayLength;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HighlightInfo {
            [MarshalAs(UnmanagedType.LPStr)]
            public string PlayerName;
            public byte Type;
            public uint Unknown1;
            public uint Unknown2;
            public Vec3 Unknown3;
            public Vec3 Unknown4;
            public Vec3 Unknown5;
            public Vec3 Up;
            public ulong HeroMasterKey;
            public ulong ItemSkinKey;
            public ulong ItemWSkinKey;
            public ulong ItemHighlightKey;
            public ulong C2Key;
            public ulong Timestamp;
            public UUID UUID;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HighlightFilter {
            public uint Unknown1;
            public uint Unknown2;
            public uint Unknown3;
            public byte Unknown4;
        }
    }
}
