using OWLib.Types;
using static STULib.Types.STULootboxReward.Common;

namespace STULib.Types.STULootboxReward {
    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0x4EE84DC0)]
    [STUInherit(Parent = typeof(STUCosmeticItem))]
    public struct HeroicIntro {
        public OWRecord AnimationResource;
        public ulong Unknown1;
        public ulong Unknown2;
    }
}
