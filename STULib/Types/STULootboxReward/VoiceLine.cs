using OWLib.Types;
using static STULib.Types.STULootboxReward.Common;

namespace STULib.Types.STULootboxReward {
    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0x090B30AB)]
    [STUInherit(Parent = typeof(STUCosmeticItem))]
    public struct VoiceLine {
        public ulong Unknown1;
        public OWRecord EffectResource;
        public OWRecord Decal;
        public OWRecord SubEffectResource;
        public ulong Unknown2;
    }
}
