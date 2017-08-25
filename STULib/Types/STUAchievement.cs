using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x0CC07049, "STUAchievement")]
    public class STUAchievement : STUInstance {
        //0916AFB9 - 4 bytes
        //0E27C815 - 8 bytes
        //2077E211 - 8 bytes
        //26A7BB03 - 4 bytes
        //4C73ADB5 - 4 bytes
        //5180E750 - 4 bytes
        //5BB9A817 - 4 bytes
        //7E7CB22B - 4 bytes
        //B9452DF9 - 4 bytes
        //C08C4427 - 8 bytes
        //FB3B13FA - 8 bytes

        // issues:
        // sometimes unk1 is a int
        // sometimes a long
        // int example: 000000000002.068
        // long example 0000000000157.068

        [STUField(0x2C54AEAF)]
        public ulong Unknown1;

        [STUField(0xCA7E6EDC)]
        public STUGUID UnkStringA;

        [STUField(0xF5087894)]
        public STUGUID Reward;

        [STUField(0x4E291DCC)]
        public uint Unknown2;

        [STUField(0x5351832E)]
        public uint ID;

        [STUField(0x628D48CC)]
        public uint Unknown3;

        [STUField(0x59D52DA5)]
        public uint Unknown4;

        [STUField(0x07DD813E)]
        public uint XPReward;

        [STUField(0x4FF98D41)]
        public uint Unknown5;

        [STUField(0xB48F1D22)]
        public STUGUID UnkStringB;

        [STUField(0x544A6A4F)]
        public STUGUID Image;

        [STUField(0x245A3F6D)]
        public STUGUID Image2;

        [STUField(0x290B2ADF)]
        public ulong Unknown6;
    }
}
