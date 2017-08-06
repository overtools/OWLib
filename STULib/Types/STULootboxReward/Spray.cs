using OWLib.Types;
using static STULib.Types.STULootboxReward.Common;

namespace STULib.Types.STULootboxReward {
    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0x15720E8A)]
    [STUInherit(Parent = typeof(STUCosmeticItem))]
    public struct Spray {
        public ulong Unknown1;
        public OWRecord EffectResource;
        public OWRecord DecalResource;
        public ulong Unknown2;
    }

    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0x6DA011A1)]
    public struct SprayMipmap {
        public OWRecord EffectResource;
        public OWRecord DecalResource;
    }
}
