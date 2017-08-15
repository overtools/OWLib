using System;
using OWLib.Types;
using static STULib.Types.Generic.Common;
using static STULib.Types.STUUnlock.Common;

namespace STULib.Types.STUUnlock {
    public class Cosmetic : STUInstance {
        public OWRecord CosmeticName;
        public OWRecord CosmeticIconResource;
        public OWRecord CosmeticUnknownRecord;
        public OWRecord CosmeticBackgroundResource;
        public OWRecord CosmeticAvailableIn;
        public ulong CosmeticUnknown1;
        public ulong CosmeticUnknown2;
        public OWRecord CosmeticDescription;
        public Rarity CosmeticRarity;
        public uint CosmeticFlags;

        [BuildVersionRange(38125)]
        // 1.12 release
        [BuildVersionRange(38024, 38044)]
        // 1.12 release
        [BuildVersionRange(37755, 37793)]
        // 1.12 release
        [BuildVersionRange(37646, 37703)]
        public ulong CosmeticUnknown3;
    }
}
