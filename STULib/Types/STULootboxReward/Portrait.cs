using OWLib.Types;

namespace STULib.Types.STULootboxReward {
    [System.Diagnostics.DebuggerDisplay(STU.DEBUG_STR)]
    [STU(Checksum = 0x3ECCEB5D)]
    public class Portrait : Cosmetic {
        public OWRecord PortraitResource;
        public OWRecord BorderResource;
        public uint Tier;
        public ushort Bracket;
        public ushort Star;
    }
}