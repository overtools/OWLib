using System.Runtime.InteropServices;
using OWLib.Types;
using static OWReplayLib.Types.Common;
using static OWReplayLib.Types.Highlight;

namespace OWReplayLib.Types {
    // https://github.com/willkirkby/overwatch-highlights/blob/master/OverwatchHighlights/Replay.cs
    public static class Replay {
        public const uint MAGIC_CONSTANT = 0x03707270; // prp3, Player RePlay v3?

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ReplayHeader {
            public uint Magic;
            public uint BuildVersion;
            public ulong MapDataKey;
            public ulong C5Key;
            public byte Flags; // TODO
            public uint Unknown2;
            public uint Unknown3;
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 0x20)]
            public byte[] Checksum;
            public uint ReplayParamLength;
            public ReplayParam Params;
            // ReplayHeaderEx if Flags & 0x4 > 0
            // Compressed Buffer, Facebook Z-Standard (zstd)
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ReplayHeaderEx {
            public uint PlayerBlockLength;
            public HighlightInfo Info;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ReplayParam {
            public uint StartFrame;
            public uint EndFrame;
            public ulong Duration;
            public ulong Start;
            public ulong End;
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayMarshalerI<HeroInfo>))]
            public HeroInfo[] HeroInfo;
        }
    }
}
