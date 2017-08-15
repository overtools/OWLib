using static STULib.Types.Generic.Common;
using static STULib.Types.STUUnlock.Common;

namespace STULib.Types.STUUnlock {
    public class Cosmetic : STUInstance {
        [STUField(0xC08C4427)]
        public STUGUID CosmeticName;

        public STUGUID CosmeticIconResource;
        public STUGUID CosmeticUnknownRecord;
        public STUGUID CosmeticBackgroundResource;
        public STUGUID CosmeticAvailableIn;
        public ulong CosmeticUnknown1;
        public ulong CosmeticUnknown2;
        [STUField(0x0E27C815)]
        public STUGUID CosmeticDescription;

        [STUField(0xFD53ECB8)]
        public Rarity CosmeticRarity;

        public uint CosmeticFlags;

        [BuildVersionRange(38125)]
        [BuildVersionRange(38024, 38044)]
        [BuildVersionRange(37755, 37793)]
        [BuildVersionRange(37646, 37703)]
        public ulong CosmeticUnknown3;
    }
}
