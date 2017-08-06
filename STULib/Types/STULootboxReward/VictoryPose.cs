using OWLib.Types;
using static STULib.Types.STULootboxReward.Common;

namespace STULib.Types.STULootboxReward {
    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0x018667E2)]
    [STUInherit(Parent = typeof(STUCosmeticItem))]
    public struct VictoryPose {
        public OWRecord PoseResource;
    }
}