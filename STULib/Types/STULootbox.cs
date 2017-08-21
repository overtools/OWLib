using OWLib;
using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x56B6D12E, "STULootbox")]
    public class STULootbox : STUInstance {
        [STUField(0x537B9B9E)]
        public STUGUID Effect;

        [STUField(0x6C7F44B7)]
        public STUGUID Effect2;

        [STUField(0x6D4304BF)]
        public STUGUID Material;

        [STUField(0x90A4CE09)]
        public STUGUID UnknownStateScriptComponent;

        [STUField(0xC08C4427)]
        public STUGUID Name;

        [STUField(0xC6C46D25)]
        public LootboxBundle[] Bundldes;

        [STUField(0xEA136681)]
        public STUGUID Effect3;

        [STUField(0xED9136D5)]
        public uint Unknown1;  // 0

        [STUField(0x581570BA)]
        public uint EventID;

        [STUField(0xC1458915)]
        public STUGUID Unknown0C3;  // 0C3 isn't in CMF

        [STUField(0xFCDFD33B)]
        public STUGUID Effect4;

        [STUField(0x40B14C7E)]
        public STUGUID Material2;

        [STUField(0x17A08ED7)]
        public byte Unknown2;

        [STUField(0xB1359AA2)]
        public byte Unknown3;

        public class LootboxBundle {
            [STUField(0x294A8C84)]
            public STUGUID String;
            [STUField(0x6AA931B6)]
            public STUGUID UnknownImage;
        }

        public string EventNameNormal => ItemEvents.GetInstance().GetEventNormal(EventID);
        public string EventName => ItemEvents.GetInstance().GetEvent(EventID);
    }
}
