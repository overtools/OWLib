using OWLib.Types;
using static STULib.Types.STULootboxReward.Common;

namespace STULib.Types.STULootboxReward {
    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0x8CDAA871)]
    [STUInherit(Parent = typeof(STUCosmeticItem))]
    public struct Icon {
        public ulong Unknown1;
        public OWRecord EffectResource;
        public OWRecord DecalResource;
        public ulong Unknown2;
    }
}
