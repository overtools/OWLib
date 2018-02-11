using STULib.Types.Generic;

namespace STULib.Types {
    [STU(0x6760479E, "STUUnlock")]
    public class STUUnlock: Common.STUInstance {
        [STUField(0xB48F1D22, "m_name")]
        public Common.STUGUID CosmeticName;

        [STUField(0xDAD2E3A2)]
        public Common.STUGUID CosmeticTextureResource;

        [STUField(0xBDB2D444)]
        public Common.STUGUID CosmeticUnknownGUID;

        [STUField(0x84F3DCC0)]
        public Common.STUGUID CosmeticEventImage;

        [STUField(0x53145FAF)]
        public Common.STUGUID CosmeticAvailableIn;

        [STUField(0xAA8E1BB0)]
        public Common.STUGUID[] CosmeticUnknownArray;  // virtual

        [STUField(0x3446F580, "m_description")]
        public Common.STUGUID CosmeticDescription;

        [STUField(0x0B1BA7C1)]
        public Common.STUGUID LeagueTeam;

        [STUField(0xBB99FCD3, "m_rarity")]
        public Enums.STUEnumRarity CosmeticRarity;

        [STUField(0x5B66C189)]
        public int m_5B66C189;

        [STUField(0x8EEF1251)]
        public int CosmeticSortOrder;

        [STUField(0x1A546C64)]
        public byte CosmeticUnknownByte;

        [STULib.STUField(0x40926C4A)]
        public byte m_40926C4A;
    }
}
