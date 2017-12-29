using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x04118DBD, "STUSound")]  // todo: bad naming is getting to be a problem
    public class STUSound : STUInstance {
        [STUField(0xF8274C10)]
        public STUGUID GUIDx02A;

        [STUField(0x1DBE70DD)]
        public STUGUID[] GUIDx04F;

        [STUField(0x57351687, EmbeddedInstance = true)]
        public STUSoundbankDataVersion Inner;

        [STUField(0x13523278)]
        public float m_13523278;

        [STUField(0xBEB35A19)]
        public float m_BEB35A19;

        [STUField(0xD138B819)]
        public float m_D138B819;

        [STUField(0x6657412F)]
        public uint m_6657412F;

        [STUField(0xACA7FBC8)]
        public float m_ACA7FBC8;

        [STUField(0x77092FDD)]
        public byte m_77092FDD;

        [STUField(0xB62657B8)]
        public byte m_B62657B8;
    }

    [STU(0x0EBAF735)]
    public class STUSoundbankDataVersion : STUInstance {
        [STUField(0xFA1FDF0D)]
        public string m_FA1FDF0D;

        [STUField(0x75754566)]
        public STUGUID Soundbank;

        [STUField(0x48A42D80)]
        public STUGUID[] Sounds;

        [STUField(0x316EBD9D)]
        public STUGUID[] SoundOther;

        [STUField(0x72F2A9FA)]
        public uint[] IDs;

        [STUField(0x5A7EC887)]
        public uint[] m_5A7EC887;

        [STUField(0xF99A0938)]
        public string m_F99A0938;

        [STUField(0x4587972B)]
        public STUGUID[] GUIDx04C;

        [STUField(0x09D4067B)]
        public STUGUID[] GUIDx0C9;

        [STUField(0xF08EDDCA)]
        public STUGUID[] GUIDx0CA;

        [STUField(0xF7805734)]
        public uint m_F7805734;

        [STUField(0xACB34EFC)]
        public float m_ACB34EFC;

        [STUField(0x5CEC8D67)]
        public uint m_5CEC8D67;

        [STUField(0xEE7B11F5)]
        public uint m_EE7B11F5;

        [STUField(0x573D3605)]
        public byte m_573D3605;
    }
}
