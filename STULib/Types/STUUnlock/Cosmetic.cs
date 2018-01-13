using static STULib.Types.Generic.Common;

namespace STULib.Types.STUUnlock {
    [STU(0x6760479E, "STUUnlock")]
    public class Cosmetic: STUInstance {
        [STUField(0xB48F1D22, "m_name")]
        public STUGUID CosmeticName;

        [STUField(0xBB99FCD3, "m_rarity")]
        public Enums.STUEnumRarity CosmeticRarity;

        [STUField(0x53145FAF)]
        public STUGUID CosmeticAvailableIn;

        [STUField(0x3446F580, "m_description")]
        public STUGUID CosmeticDescription;

        [STUField(0x84F3DCC0)]
        public STUGUID CosmeticEventImage;

        [STUField(0x1A546C64)]
        public byte CosmeticUnknownByte;

        [STUField(0xAA8E1BB0)]
        public STUGUID[] CosmeticUnknownArray;  // virtual

        [STUField(0x8EEF1251)]
        public int CosmeticSortOrder;

        [STUField(0xDAD2E3A2)]
        public STUGUID CosmeticTextureResource;

        [STUField(0xBDB2D444)]
        public STUGUID CosmeticUnknownGUID;
        
        [STUField(0x0B1BA7C1)]
        public STUGUID LeagueTeam;

        [STUField(0x5B66C189)]
        public int m_5B66C189;
    }
}
