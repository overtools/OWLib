using static STULib.Types.STULootboxReward.Common;

namespace STULib.Types.STULootboxReward {
    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0x0BCAF9C9)]
    [STUInherit(Parent = typeof(STUCosmeticItem))]
    public struct Credit {
        public ulong Value;
    }
}
