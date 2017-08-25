using static STULib.Types.Generic.Common;

namespace STULib.Types {
    public enum AbilityType : uint {
        WEAPON = 0,
        NORMAL = 1,
        PASSIVE = 2,
        ULTIMATE = 3
    }

    [STU(0x07A0E32F)]
    public class STUAbilityInfo : STUInstance {
        [STUField(0x2C54AEAF)]
        public AbilityType AbilityType;

        [STUField(0x3CD6DC1E)]
        public STUGUID Image;

        [STUField(0xB48F1D22)]
        public STUGUID Name;

        [STUField(0xC8D38D7B)]
        public STUGUID TurorialVideo;

        [STUField(0xCA7E6EDC)]
        public STUGUID Description;

        [STUField(0xFC33191B)]
        public STUGUID Unknown096_1;

        [STUField(0x0E679979)]
        public uint WeaponIndex;  // 0 = 0th weapon (sometimes the first weapon is 1, sometimes 0) 1 = first weapon, 2 = second weapon...
        // Doomfist's weapon is 0, every other is 1

        [STUField(0x7E3ED979)]
        public uint Unknown3;  // always 0?

        [STUField(0x9290B942)]
        public STUGUID Unknown096_2;

        [STUField(0xB1124918)]
        public uint Unknown4;

    }
}
