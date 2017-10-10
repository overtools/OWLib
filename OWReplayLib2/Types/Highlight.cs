using System.Runtime.InteropServices;
using OWLib.Types;
using OWReplayLib.Types;

namespace OWReplayLib2.Types {
    public static class Highlight {
        // todo: move types over
        
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HighlightHeader {
            public uint Magic;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
            public byte[] Checksum;
            public uint DataLength;
            public ulonglong Unknown1;
            public ulonglong Unknown2;
            public uint ReplayVersion;
            public uint BuildVersion;
            public uint PlayerId;
            public uint Unknown3;
            public OWReplayLib.Types.Highlight.HighlightFlags Flags;
            public Common.FileReference MapDataKey;
            public Common.FileReference C5Key;
            // [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayMarshaler<HighlightInfo, int>))]
            // [MarshalAs(UnmanagedType.LPArray , ArraySubType = UnmanagedType.Struct)]
            // public HighlightInfo[] Info;
            public int tempCount;

            // [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayMarshaler<Common.HeroInfo, int>))]
            // public Common.HeroInfo[] HeroInfo;
            // public int tempHeroCount;
            // public ulong Unknown4;
            // [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayMarshaler<OWReplayLib.Types.Highlight.HighlightFilter, int>))]
            // public OWReplayLib.Types.Highlight.HighlightFilter[] Filters;
            // public int ReplayLength;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HighlightInfo {
            // [MarshalAs(UnmanagedType.LPStr)]
            // [MarshalAs(UnmanagedType.LPStr)]
            public string PlayerName;
            
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1)]
            public char[] te;
            
            // public byte Type;
            // public uint Unknown1;
            // public uint Unknown2;
            // public Vec3 Unknown3;
            // public Vec3 Unknown4;
            // public Vec3 Unknown5;
            // public Vec3 Up;
            // public Common.FileReference HeroMasterKey;
            // public Common.FileReference ItemSkinKey;
            // public Common.FileReference ItemWSkinKey;
            // public Common.FileReference ItemHighlightKey;
            // public Common.FileReference C2Key;
            // public ulong Timestamp;
            // public UUID UUID;
        }
    }
}