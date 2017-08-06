using OWLib.Types;

namespace STULib.Types.STULootboxReward {
    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0x090B30AB)]
    public class VoiceLine : Cosmetic {
        public ulong Unknown1;
        public OWRecord EffectResource;
        public OWRecord Decal;
        public OWRecord SubEffectResource;
        public ulong Unknown2;
    }
}
