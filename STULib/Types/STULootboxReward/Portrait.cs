using OWLib.Types;
using static STULib.Types.STULootboxReward.Common;

namespace STULib.Types.STULootboxReward {
    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0x3ECCEB5D)]
    [STUInherit(Parent = typeof(STUCosmeticItem))]
    public struct Portrait {
        public OWRecord PortraitResource;
        public OWRecord BorderResource;
        public uint Tier;
        public ushort Bracket;
        public ushort Star;
    }
}