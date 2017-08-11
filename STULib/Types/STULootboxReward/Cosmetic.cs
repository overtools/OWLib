using OWLib.Types;
using static STULib.Types.Generic;
using static STULib.Types.STULootboxReward.Common;

namespace STULib.Types.STULootboxReward {
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
        public ulong CosmeticUnknown3;
    }
}
