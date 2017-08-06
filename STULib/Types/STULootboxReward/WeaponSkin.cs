using static STULib.Types.STULootboxReward.Common;

namespace STULib.Types.STULootboxReward {
    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0x01609B4D)]
    [STUInherit(Parent = typeof(STUCosmeticItem))]
    public struct WeaponSkin {
        public ulong Index;
        public ulong Unknown1;
    }
}