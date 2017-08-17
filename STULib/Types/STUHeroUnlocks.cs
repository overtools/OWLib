using static STULib.Types.Generic.Common;
#pragma warning disable 169

namespace STULib.Types {
    [STU(0x33F56AC1)]
    public class STUHeroUnlocks : STUInstance {
        [STUField(STUVersionOnly = new uint[] {1})]
        private long padding0;

        [STUField(0x5C57AB57)]
        public UnlockInfo SystemUnlocks;

        [STUField(STUVersionOnly = new uint[] {1})]
        private long padding1;
        [STUField(STUVersionOnly = new uint[] {1})]
        private long padding2;
        [STUField(STUVersionOnly = new uint[] {1})]
        private long padding3;

        [STUField(0x719E981B)]
        public UnlockInfo[] Unlocks;

        [STUField(0xB772FEE7)]
        public EventUnlockInfo[] LootboxUnlocks;

        public class EventUnlockInfo {
            [STUField(STUVersionOnly = new uint[] {1})]
            private long padding0;

            [STUField(0x719E981B, Padding = 8)]
            public UnlockInfo Data;

            [STUField(0x581570BA)]
            public uint Event;

            [STUField(STUVersionOnly = new uint[] {1})]
            private uint padding1;
        }


        public class UnlockInfo {
            [STUField(0x719E981B, Padding = 8)]
            public STUGUID[] Unlocks;
        }
    }
}
