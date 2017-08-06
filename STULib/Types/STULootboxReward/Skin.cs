using OWLib.Types;
using static STULib.Types.STULootboxReward.Common;

namespace STULib.Types.STULootboxReward {
    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0x8B9DEB02)]
    [STUInherit(Parent = typeof(STUCosmeticItem))]
    public struct Skin {
        public OWRecord SkinResource;
    }
}