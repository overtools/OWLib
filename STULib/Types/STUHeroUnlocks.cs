using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x33F56AC1)]
    public class STUHeroUnlocks : STUInstance {
        [STUField(0x5C57AB57)]
        public UnlockInfo SystemUnlocks;

        [STUField(0x719E981B)]
        public UnlockInfo[] Unlocks;

        [STUField(0xB772FEE7)]
        public EventUnlockInfo[] LootboxUnlocks;

        public class EventUnlockInfo {
            [STUField(0x581570BA)]
            public uint Event;

            [STUField(0x719E981B, Padding = 8)]
            public UnlockInfo Data;
        }


        public class UnlockInfo {
            [STUField(0x719E981B, Padding = 8)]
            public STUGUID[] Unlocks;
        }
    }
}
