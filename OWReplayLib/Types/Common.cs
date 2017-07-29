using System;
using System.Runtime.InteropServices;
using OWLib.Types;

namespace OWReplayLib.Types {
    public static class Common {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        // https://github.com/willkirkby/overwatch-highlights/blob/master/OverwatchHighlights/HeroWithUnlockables.cs
        public struct HeroInfo {
            public uint SkinId;
            public uint WSkinId;
            public uint HighlightId;
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayMarshalerI<uint>))]
            public uint[] SprayIds;
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayMarshalerI<uint>))]
            public uint[] VoiceLineIds;
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayMarshalerI<uint>))]
            public uint[] EmoteIds;
            public ulong HeroMasterKey;
        }
    }
}
