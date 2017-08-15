using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x33F56AC1)]
    public class STUHeroUnlocks : STUInstance {
        
        [STUField(0x17281EEB)]
        public NestedGUID SystemGroup;

        [STUField(0x5C57AB57)]
        public NestedGUID DefaultGroup;

        [STUField(0x719E981B)] // m_unlocks
        public UnlockCategory Unlocks;

        public class NestedGUID {
            [STUField(0x719E981B, Padding = 8)] // m_unlocks
            public STUGUID[] Unlocks;
        }

        public class UnlockCategory {
            public uint Unknown;
        }
    }
}
