using static STULib.Types.Generic.Common;

namespace STULib.Types.STUUnlock {
    [STU(0x3ECCEB5D)]
    public class Portrait : Cosmetic {
        public STUGUID PortraitResource;
        public STUGUID BorderResource;
        public uint Tier;
        public ushort Bracket;
        public ushort Star;
    }
}