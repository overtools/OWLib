using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x02514B6A)]
    public class STUMap : STUInstance {
        [STUField(0x389CB894)]
        public STUGUID StringDescriptionA;

        [STUField(0x44D13CC2)]
        public uint Unknown1;

        [STUField(0x4E87690F)]
        public STUGUID MapData;

        [STUField(0x5AFE2F61)]
        public STUGUID StringStateA;

        [STUField(0x5DB91CE2)]
        public STUGUID StringName;

        [STUField(0x86C1CFAB)]
        public STUGUID ImageA;

        [STUField(0x8EBADA44)]
        public STUGUID StringStateB;

        [STUField(0x9386E669)]
        public STUGUID ImageB;

        [STUField(0x956158FF)]
        public STUGUID UnknownEffect;

        [STUField(0xA0AE2E3E)]
        public STUGUID UnknownEffect2;

        [STUField(0xA125818B)]
        public uint Unknown2;

        [STUField(0xACB95597)]
        public STUGUID StringDescriptionB;

        [STUField(0xAF869CEC)]
        public uint Unknown3;

        [STUField(0xC6599DEB)]
        public STUGUID ImageC;  // todo, messed up in 0000000000D4.09F

        [STUField(0xD608E9F3)]
        public uint Unknown4;

        [STUField(0xDDC37F3D)]
        public STUGUID MapData2;

        [STUField(0xEBCFAD22)]
        public STUGUID StringSubline;

        [STUField(0x7F5B54B2)]
        public STUGUID SoundMaster;

        [STUField(0x1C706502)]
        public STUGUID StringNameB;

        // [STUField(0x5FF3ACFB)]
        // public uint Unknown5;  // 0 bytes

        [STUField(0x1DD3A0CD)]
        public uint Unknown6;

        [STUField(0x762B6796)]
        public uint Unknown7;
    }
}
