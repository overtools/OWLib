//using System;
//using static STULib.Types.Generic.Common;

//namespace STULib.Types {
//    //public enum HeroType : uint {
//    //    OFFENCE = 27,
//    //    DEFENCE = 26,
//    //    TANK = 25,
//    //    SUPPORT = 24,
//    //    FRIELDLY_AI = 20,
//    //    HOSTILE_AI = 28
//    //}
//    public enum HeroPlayerType : byte {
//        AI,
//        PLAYER
//    }

//    public enum HeroAvailableAt : byte {
//        POST_LAUNCH,
//        LAUNCH
//    }

//    [STU(0x7C3457DC)]
//    public class STUHero : STUInstance {
//        [STUField(0x0EDCE350)]
//        public STUGUID Name;

//        [STUField(0x26D71549)]
//        public STUGUID StateScriptComponentA;

//        [STUField(0x2C54AEAF)]
//        public STUGUID Unknown01C;

//        [STUField(0x322C521A)]
//        public STUGUID StateScriptComponentB;

//        [STUField(0x3446F580)]
//        public STUGUID Description;

//        [STUField(0x38BFB46C)]
//        public STUGUID EncryptionKey;

//        [STUField(0x44D13CC2)]
//        public uint ID;  // not on NPCs

//        [STUField(0x485AA39C)]
//        public STUGUID Inventory;

//        [STUField(0x62746D34)]
//        public HeroPlayerType PlayerType;

//        [STUField(0x646B9249)]
//        public HeroAvailableAt AvailableAt;  // not on NPCs

//        [STUField(0x77FED604)]
//        public uint Unknown11;  // 0

//        [STUField(0x7D88A63A)]
//        public uint Unknown12;

//        [STUField(0x8125713E)]
//        public STUGUID StateScriptComponentC;

//        [STUField(0x84625AA3)] // 0 bytes
//        public sdfsdf[] sdfd;
//        public class sdfsdf { }

//        [STUField(0x950BBA06)]
//        public STUGUID StateScriptComponentD;

//        //[STUField(0xA341183E)] // 0 bytes

//        [STUField(0xAC91BECC)]
//        public STUGUID StateScriptComponentE;

//        [STUField(0xB7A1D145)]
//        // public HeroType Unknown16;                                                 // todo: is this hero type
//        public uint Unknown16;  // seems to be type/class, but it's messed up

//        //[STUField(0xC2FE396F)] // 0 bytes

//        [STUField(0xD696F2F6)]
//        public STUGUID ImageA;

//        [STUField(0xD90B256D)]
//        public STUGUID ImageB;

//        [STUField(0xDAD2E3A2)]
//        public STUGUID ImageC;

//        [STUField(0xE04197AF)]
//        public uint Unknown20;  // count of something, new heros have low count, npc=0

//        // [STUField(0xE25DDDA1)]
//        // public byte Unknown21;  // todo: proper type

//        [STUField(0xEA6FF023)]
//        public STUGUID ImageD;

//        [STUField(0xF2D8DE15)]
//        public uint Unknown23;

//        // [STUField(0xFC833C02)] // 0 bytes
//        // public Hero1[] sdfsd;
//        // public HeroStaticsicThingy[] HeroStaticsicThingies;  // todo: name

//        // [STUField(0xFCD2B649)] // 0 bytes
//        // public Hero2[] aaaaaaaaaa;

//        [STUField(0xFF3C2071)]
//        public uint Unknown24;  // mostly 0

//        // [STUField(0x418F797D)] // 0 bytes
//        // public Hero3[] aaaaaaaaaa2;

//        [STUField(0xD12CB4EA)]
//        public uint Unknown25;  // mostly 0

//        [STUField(0xAF4EC410)]
//        public bool IsTank;

//        [STUField(0xBB16810A)]
//        public uint Unknown27;  // only on Zomnic and Zombardier, super tempted to call this "IsZombie"

//        // [STUField(0xE1258EC1)] // 0 bytes
//        // public uint hello;

//        public class Hero1 {
//            //[STUField(0x0029461B)] //- 8 bytes
//            //public STUGUID hh;
//            //[STUField(0x5E9665E3)] //- 8 bytes
//            //public STUGUID hh2;
//            //[STUField(0x7B95C2A2)] //- 1 bytes
//            //public byte b;
//            //[STUField(0x88F5CF3E)] //- 4 bytes
//            //public uint u;
//        }

//        public class HeroStaticsicThingy {
//            [STUField(0x07EC21F2)]
//            public STUGUID StatisticA;
//            [STUField(0xBC4326FF)]
//            public STUGUID StatisticB;
//            [STUField(0xB5B91051)]
//            public STUGUID StatisticC;
//        }

//        public class Hero2 {
//            [STUField(0x38F997AB)]
//            public STUGUID StateScriptComponent;
//            //[STU: STULib.Types.STUHero+Hero2]: Unknown field 0827AB28 (12 bytes)
//            //[STU: STULib.Types.STUHero+Hero2]: Unknown field 18789D20 (12 bytes)
//            //[STU: STULib.Types.STUHero+Hero2]: Unknown field 38F997AB(8 bytes)
//            //[STU: STULib.Types.STUHero+Hero2]: Unknown field AF9D3A0C(16 bytes)
//            //[STU: STULib.Types.STUHero+Hero2]: Unknown field FF72C038(12 bytes)
//        }

//        public class Hero3 {
//            //[STU: STULib.Types.STUHero+Hero3]: Unknown field 9A97C666(4 bytes)
//            //[STU: STULib.Types.STUHero+Hero3]: Unknown field B44A42A0(4 bytes)
//            [STUField(0x9A97C666)]
//            public uint field1;

//            [STUField(0xB44A42A0)]
//            public uint field2;

//        }
//    }
//}

//// File auto generated by STUHashTool
//using static STULib.Types.Generic.Common;

//namespace STULib.Types {
//    [STU(0x7C3457DC)]
//    public class STU_7C3457DC : STUInstance {
//        [STUField(0x0EDCE350)]
//        public STUGUID Unknown1;  //todo: check if long

//        [STUField(0x26D71549)]
//        public STUGUID Unknown2;  //todo: check if long

//        [STUField(0x2C54AEAF)]
//        public STUGUID Unknown3;  //todo: check if long

//        [STUField(0x322C521A)]
//        public STUGUID Unknown4;  //todo: check if long

//        [STUField(0x3446F580)]
//        public STUGUID Unknown5;  //todo: check if long

//        [STUField(0x38BFB46C)]
//        public STUGUID Unknown6;  //todo: check if long

//        [STUField(0x44D13CC2)]
//        public uint Unknown7;

//        [STUField(0x485AA39C)]
//        public STUGUID Unknown8;  //todo: check if long

//        [STUField(0x62746D34)]
//        public byte Unknown9;  //todo: check if char

//        [STUField(0x646B9249)]
//        public byte Unknown10;  //todo: check if char

//        [STUField(0x77FED604)]
//        public uint Unknown11;

//        [STUField(0x7D88A63A)]
//        public uint Unknown12;

//        [STUField(0x8125713E)]
//        public STUGUID Unknown13;  //todo: check if long

//        [STUField(0x84625AA3)]
//        public STU_7C3457DC_84625AA3[] Unknown14;  // todo: check nested array

//        [STUField(0x950BBA06)]
//        public STUGUID Unknown15;  //todo: check if long

//        //[STUField(0xA341183E)]  // unhandled array item size: 0

//        [STUField(0xAC91BECC)]
//        public STUGUID Unknown17;  //todo: check if long

//        [STUField(0xB7A1D145)]
//        public uint Unknown18;

//        [STUField(0xC2FE396F)]  // unhandled array item size: 0
//        public C2FE396F[] fsdfsd;
//        public class C2FE396F { }

//        [STUField(0xD696F2F6)]
//        public STUGUID Unknown20;  //todo: check if long

//        [STUField(0xD90B256D)]
//        public STUGUID Unknown21;  //todo: check if long

//        [STUField(0xDAD2E3A2)]
//        public STUGUID Unknown22;  //todo: check if long

//        [STUField(0xE04197AF)]
//        public uint Unknown23;

//        //[STUField(0xE25DDDA1)]  // unhandled array item size: 16

//        [STUField(0xEA6FF023)]
//        public STUGUID Unknown25;  //todo: check if long

//        [STUField(0xF2D8DE15)]
//        public uint Unknown26;

//        //[STUField(0xFC833C02)]  // unhandled array item size: 0

//        //[STUField(0xFCD2B649)]  // unhandled array item size: 0

//        [STUField(0xFF3C2071)]
//        public uint Unknown29;

//        [STUField(0x418F797D)]
//        public STU_7C3457DC_418F797D[] Unknown30;  // todo: check nested

//        [STUField(0xD12CB4EA)]
//        public uint Unknown31;

//        [STUField(0xAF4EC410)]
//        public uint Unknown32;

//        [STUField(0xBB16810A)]
//        public uint Unknown33;

//        //[STUField(0xE1258EC1)]  // unhandled array item size: 0
//        public class STU_7C3457DC_84625AA3 {
//            [STUField(0x0029461B)]
//            public STUGUID Unknown1;  //todo: check if long

//            [STUField(0x5E9665E3)]
//            public STUGUID Unknown2;  //todo: check if long

//            [STUField(0x7B95C2A2)]
//            public byte Unknown3;  //todo: check if char

//            [STUField(0x88F5CF3E)]
//            public byte[] Unknown4;  // todo: check array  //todo: check if char
//        }


//        public class STU_7C3457DC_418F797D {
//            [STUField(0x9A97C666)]
//            public uint Unknown1;

//            [STUField(0xB44A42A0)]
//            public uint Unknown2;
//        }
//    }
//}

// File auto generated by STUHashTool
using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x7C3457DC)]
    public class STU_7C3457DC : STUInstance {
        [STUField(0x0EDCE350)]
        public ulong Unknown1;  //todo: check if STUGUID

        [STUField(0x26D71549)]
        public ulong Unknown2;  //todo: check if STUGUID

        [STUField(0x2C54AEAF)]
        public ulong Unknown3;  //todo: check if STUGUID

        [STUField(0x322C521A)]
        public ulong Unknown4;  //todo: check if STUGUID

        [STUField(0x3446F580)]
        public ulong Unknown5;  //todo: check if STUGUID

        [STUField(0x38BFB46C)]
        public ulong Unknown6;  //todo: check if STUGUID

        [STUField(0x44D13CC2)]
        public uint Unknown7;

        [STUField(0x485AA39C)]
        public ulong Unknown8;  //todo: check if STUGUID

        [STUField(0x62746D34)]
        public byte Unknown9;  //todo: check if char

        [STUField(0x646B9249)]
        public byte Unknown10;  //todo: check if char

        [STUField(0x77FED604)]
        public byte[] Unknown11;  //todo: check if char

        [STUField(0x7D88A63A)]
        public uint Unknown12;

        [STUField(0x8125713E)]
        public ulong Unknown13;  //todo: check if STUGUID

        [STUField(0x84625AA3)]
        public STU_7C3457DC_84625AA3[] Unknown14;  // todo: check nested array

        [STUField(0x950BBA06)]
        public ulong Unknown15;  //todo: check if STUGUID

        [STUField(0xA341183E)]
        public STU_7C3457DC_A341183E[] Unknown16;  // todo: check nested array

        [STUField(0xAC91BECC)]
        public ulong Unknown17;  //todo: check if STUGUID

        [STUField(0xB7A1D145)]
        public uint Unknown18;

        [STUField(0xC2FE396F)]
        public STU_7C3457DC_C2FE396F[] Unknown19;  // todo: check nested array

        [STUField(0xD696F2F6)]
        public ulong Unknown20;  //todo: check if STUGUID

        [STUField(0xD90B256D)]
        public ulong Unknown21;  //todo: check if STUGUID

        [STUField(0xDAD2E3A2)]
        public ulong Unknown22;  //todo: check if STUGUID

        [STUField(0xE04197AF)]
        public byte[] Unknown23;  //todo: check if char

        //[STUField(0xE25DDDA1)]  // unhandled field size: 16

        [STUField(0xEA6FF023)]
        public ulong Unknown25;  //todo: check if STUGUID

        [STUField(0xF2D8DE15)]
        public byte[] Unknown26;  //todo: check if char

        [STUField(0xFC833C02)]
        public STU_7C3457DC_FC833C02[] Unknown27;  // todo: check nested array

        [STUField(0xFCD2B649)]
        public STU_7C3457DC_FCD2B649[] Unknown28;  // todo: check nested array

        [STUField(0xFF3C2071)]
        public byte[] Unknown29;  //todo: check if char

        [STUField(0x418F797D)]
        public STU_7C3457DC_418F797D[] Unknown30;  // todo: check nested array

        [STUField(0xD12CB4EA)]
        public byte[] Unknown31;  //todo: check if char

        [STUField(0xAF4EC410)]
        public uint Unknown32;

        [STUField(0xBB16810A)]
        public uint Unknown33;

        [STUField(0xE1258EC1)]
        public STU_7C3457DC_E1258EC1[] Unknown34;  // todo: check nested array

        public class STU_7C3457DC_84625AA3 {
            [STUField(0x0029461B)]
            public ulong Unknown1;  //todo: check if STUGUID

            [STUField(0x5E9665E3)]
            public ulong Unknown2;  //todo: check if STUGUID

            [STUField(0x7B95C2A2)]
            public byte Unknown3;  //todo: check if char

            [STUField(0x88F5CF3E)]
            public byte[] Unknown4;  //todo: check if char
        }

        public class STU_7C3457DC_A341183E {
            [STUField(0x118D9D9F)]
            public STU_7C3457DC_A341183E_118D9D9F[] Unknown1;  // todo: check nested array

            [STUField(0xEB4F2408)]
            public ulong Unknown2;  //todo: check if STUGUID

            public class STU_7C3457DC_A341183E_118D9D9F {
                [STUField(0x07EC21F2)]
                public ulong Unknown1;  //todo: check if STUGUID

                [STUField(0xB5B91051)]
                public ulong Unknown2;  //todo: check if STUGUID

                [STUField(0xBC4326FF)]
                public ulong Unknown3;  //todo: check if STUGUID
            }
        }

        public class STU_7C3457DC_C2FE396F {
            //[STUField(0x0827AB28)]  // unhandled field size: 12

            //[STUField(0x18789D20)]  // unhandled field size: 12

            [STUField(0x38F997AB)]
            public ulong Unknown3;  //todo: check if STUGUID

            //[STUField(0xAF9D3A0C)]  // unhandled field size: 16

            //[STUField(0xFF72C038)]  // unhandled field size: 12
        }

        public class STU_7C3457DC_FC833C02 {
            [STUField(0x07EC21F2)]
            public ulong Unknown1;  //todo: check if STUGUID

            [STUField(0xB5B91051)]
            public ulong Unknown2;  //todo: check if STUGUID

            [STUField(0xBC4326FF)]
            public ulong Unknown3;  //todo: check if STUGUID
        }

        public class STU_7C3457DC_FCD2B649 {
            //[STUField(0x0827AB28)]  // unhandled field size: 12

            //[STUField(0x18789D20)]  // unhandled field size: 12

            [STUField(0x38F997AB)]
            public ulong Unknown3;  //todo: check if STUGUID

            //[STUField(0xAF9D3A0C)]  // unhandled field size: 16

            //[STUField(0xFF72C038)]  // unhandled field size: 12
        }

        public class STU_7C3457DC_418F797D {
            [STUField(0x9A97C666)]
            public uint Unknown1;

            [STUField(0xB44A42A0)]
            public uint Unknown2;
        }

        public class STU_7C3457DC_E1258EC1 {
            [STUField(0x925E7392)]
            public uint Unknown1;

            [STUField(0x96D9482C)]
            public byte[] Unknown2;  //todo: check if char
        }
    }
}