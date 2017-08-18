using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x7CE5C1B2, "STUAchievement")]
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

        [STUField(0x0916AFB9)]
        public uint Unknown1;

        [STUField(0x0E27C815)]
        public STUGUID UnkStringA;

        [STUField(0x2077E211)]
        public STUGUID Reward;

        [STUField(0x26A7BB03)]
        public uint Unknown2;

        [STUField(0x4C73ADB5)]
        public uint ID;

        [STUField(0x5180E750)]
        public uint Unknown3;

        [STUField(0x5BB9A817)]
        public uint Unknown4;

        [STUField(0x7E7CB22B)]
        public uint XPReward;

        [STUField(0xB9452DF9)]
        public uint Unknown5;

        [STUField(0xC08C4427)]
        public STUGUID UnkStringB;

        [STUField(0xFB3B13FA)]
        public STUGUID Image;




    }
}
