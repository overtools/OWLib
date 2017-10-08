using System;
using OWLib.Types;
using OWReplayLib.Serializer;

namespace OWReplayLib.Types {
    public static class Highlight {
        public class FillerStruct : ReadableData {
            public uint Unknown62;
            public uint Unknown63;
            public uint Unknown64;
            public byte Unknown65;
        }
        
        [Flags]
        public enum HighlightFlags {
            Top5Highlight   = 0x1, // Displayed in the "Top 5" section
            ManualHighlight = 0x2, // Displayed in the "36 manual highlights" section
            IsNotNew        = 0x4, // if false, game will display "New" label
            HasBeenExported = 0x8 // if true, game will display "Recorded" label
        }

        public class HighlightHeaderNew : ReadableData {
            [Serializer.Types.Magic24(0x6C6870)] // { 'p', 'h', 'l' }
            public uint Magic;
            public byte FormatVersion;
            //[Serializer.Types.FixedSizeArray(typeof(byte), 32)]
            //public byte[] Checksum;
            public Checksum Checksum;
            public uint DataLength;
            public ulonglong Unknown1;
            public ulonglong Unknown2;
            public uint ReplayVersion;
            public uint BuildVersion;
            public uint PlayerId;
            public uint Unknown3;
            public HighlightFlags Flags;
            public Common.FileReference MapDataKey;
            public Common.FileReference Gamemode;
            [Serializer.Types.DynamicSizeArray(typeof(int), typeof(HighlightInfoNew))]
            public HighlightInfoNew[] Info;
            [Serializer.Types.DynamicSizeArray(typeof(int), typeof(Common.HeroInfo))]
            public Common.HeroInfo[] HeroInfo;
            public uint Unknown4;
            public uint Unknown5;
            [Serializer.Types.DynamicSizeArray(typeof(byte), typeof(FillerStruct))]
            public FillerStruct[] FillerStructs;
            public int ReplayLength;
            public Replay Replay;
        }

        public class Replay : ReadableData {
            [Serializer.Types.Magic24(0x707270)] // { 'p', 'r', 'p' }
            public uint Magic;
            public byte FormatVersion;
            public uint BuildNumber;
            public Common.FileReference Map;
            public Common.FileReference Gamemode;
            public byte Unknown1;
            public uint Unknown2;
            public uint Unknown3;
            // [Serializer.Types.FixedSizeArray(typeof(byte), 32)]
            // public byte[] MapChecksum;
            public Checksum MapChecksum;
            public int ParamsBlockLength;
            public ReplayParams Params;
            [Serializer.Types.Conditional("(helper.BitwiseAnd(Unknown1, 4)) != 0", new [] {"Unknown1"})]
            public int HighlightInfoLength;
            [Serializer.Types.Conditional("(helper.BitwiseAnd(Unknown1, 4)) != 0", new [] {"Unknown1"})]
            public HighlightInfoNew HighlightInfo;
            [Serializer.Types.ZstdBuffer(Serializer.Types.ZstdBufferSize.StreamEnd)]
            public byte[] DecompressedBuffer;           
        }

        public class ReplayParams : ReadableData {
            public uint StartFrame;
            public uint EndFrame;
            public ulong ExpectedDurationMS;
            public ulong StartMS;
            public ulong EndMS;
            [Serializer.Types.DynamicSizeArray(typeof(int), typeof(Common.HeroInfo))]
            public Common.HeroInfo[] HeroInfo;
        }
        
        public class HighlightInfoNew : ReadableData {
            [Serializer.Types.NullPaddedString]
            public string PlayerName;
            
            // public byte UnknownByte;
            
            public byte Type;
            public uint Unknown1;
            public uint Unknown2;
            public float Unknown3;
            public float Unknown4;
            public uint Unknown5;
            public Vec3 Position;
            public Vec3 Direction;
            public Vec3 Up;
            public Common.FileReference HeroMasterKey;
            public Common.FileReference ItemSkinKey;
            public Common.FileReference ItemWSkinKey;
            public Common.FileReference HighlightIntro;
            public Common.FileReference HighlightType;
            public ulong Timestamp;
            public UUID UUID;
        }
    }
}