using static STULib.Types.Generic.Common;
using static STULib.Types.STUUnlock.Common;

namespace STULib.Types.STUUnlock {
    public class Cosmetic : STUInstance {
        [STUField(0xC08C4427)] // m_name
        public STUGUID CosmeticName;

        //[STUField(0)] // ?
        public STUGUID CosmeticIconResource;
        public STUGUID CosmeticUnknownRecord;
        [STUField(0xCC8FD7CE)] // ?
        public STUGUID CosmeticBackgroundResource;
        [STUField(0x912C9001)] // ?
        public STUGUID CosmeticAvailableIn;
        public ulong CosmeticUnknown1;
        public ulong CosmeticUnknown2;
        [STUField(0x0E27C815)] // m_description
        public STUGUID CosmeticDescription;

        [STUField(0xFD53ECB8)] // m_rarity
        public Rarity CosmeticRarity;
        
        [STUField(0x9976BC2A)] // ?
        public uint CosmeticFlags;

        [BuildVersionRange(38125)]
        [BuildVersionRange(38024, 38044)]
        [BuildVersionRange(37755, 37793)]
        [BuildVersionRange(37646, 37703)]
        public ulong CosmeticUnknown3;
    }
}
