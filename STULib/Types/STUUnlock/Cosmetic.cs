using System;
using OWLib.Types;
using static STULib.Types.Generic.Common;
using static STULib.Types.STUUnlock.Common;

namespace STULib.Types.STUUnlock {
    public class Cosmetic : STUInstance {
        [STUField(0xC08C4427)] public OWRecord CosmeticName;
        public OWRecord CosmeticIconResource;
        public OWRecord CosmeticUnknownRecord;
        public OWRecord CosmeticBackgroundResource;
        public OWRecord CosmeticAvailableIn;
        public ulong CosmeticUnknown1;
        public ulong CosmeticUnknown2;
        public OWRecord CosmeticDescription;
        public Rarity CosmeticRarity;
        public uint CosmeticFlags;

        [BuildVersionRange(38125)] [BuildVersionRange(38024, 38044)] [BuildVersionRange(37755, 37793)]
        [BuildVersionRange(37646, 37703)] public ulong CosmeticUnknown3;
    }
}
