using static STULib.Types.Generic.Common;

namespace STULib.Types.STUUnlock {
    public class Cosmetic: STUInstance {
        [STUField(0xB48F1D22, "m_name")]
        public STUGUID CosmeticName;
        
        [STUField(0xBB99FCD3, "m_rarity")]
        public Common.Rarity CosmeticRarity;
        
        [STUField(0x53145FAF)]
        public STUGUID CosmeticAvailableIn;
        
        [STUField(0x3446F580, "m_description")]
        public STUGUID CosmeticDescription;
        
        [STUField(0x84F3DCC0)]
        public STUGUID CosmeticImage;  // todo: is this the event image?
        
        [STUField(0x1A546C64)]
        public byte CosmeticUnknownByte;
        
        [STUField(0xAA8E1BB0)]
        public STUGUID[] CosmeticUnknownArray;  // virtual
        
        [STUField(0x1B25AB90)]
        public STU_5C713BD4 CosmeticUnknownNested;
    }
}