using OWLib.Types;
using static STULib.Types.STULootboxReward.Common;

namespace STULib.Types.STULootboxReward {
    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0xE533D614)]
    [STUInherit(Parent = typeof(STUCosmeticItem))]
    public class Emote {
        public OWRecord UnknownRecord014;
        public OWRecord AnimationResource;
        public OWRecord UnknownRecord;
        public OWRecord[] SubData;
        public long Unknown1;
        public uint Unknown2;
        public float Duration;
        public Vec4 Offset;
    }
}
