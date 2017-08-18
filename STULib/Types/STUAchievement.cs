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

        [STUField(0x0916AFB9)]
        public STUGUID Name;

        [STUField(0x0E27C815)]
        public STUGUID Name2;

        [STUField(0x2077E211)]
        public STUGUID Name3;

        [STUField(0x26A7BB03)]
        public STUGUID Name4;

        [STUField(0x4C73ADB5)]
        public STUGUID Name5;

        [STUField(0x5180E750)]
        public STUGUID Name6;

        [STUField(0x5BB9A817)]
        public STUGUID Name7;

        // [STUField(0x7E7CB22B)]
        // public STUGUID Name8;

        // [STUField(0xB9452DF9)]
        // public STUGUID Name9;

        // [STUField(0xC08C4427)]
        // public STUGUID NameA;

        // [STUField(0xFB3B13FA)]
        // public STUGUID NameB;




    }
}
