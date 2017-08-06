using OWLib.Types;
using static STULib.Types.Generic;

namespace STULib.Types.STULootboxReward {
    public static class Common {
        [STUInherit(Parent = typeof(STUInstance))]
        public struct STUCosmeticItem {
            public OWRecord Name;
            public OWRecord IconResource;
            public OWRecord UnknownRecord;
            public OWRecord BackgroundResource;
            public OWRecord AvailableIn;
            public ulong Unknown1;
            public ulong Unknown2;
            public OWRecord Description;
            public InventoryRarity Rarity;
            public uint Flags;
            public ulong Unknown3;
        }

        public enum InventoryRarity : uint {
            Common = 0,
            Rare = 1,
            Epic = 2,
            Legendary = 3
        }
    }
}
