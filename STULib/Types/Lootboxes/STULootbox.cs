using OWLib;
using STULib.Types.Enums;
using static STULib.Types.Generic.Common;

namespace STULib.Types.Lootboxes {
    [STU(0x56B6D12E, "STULootbox")]
    public class STULootbox : STUInstance {
        [STUField(0xFEC3ED62)]
        public STUGUID Effect1;

        [STUField(0x3970E137)]
        public STUGUID Effect2;

        [STUField(0x041CE51F)]
        public STUGUID Material;

        [STUField(0xCBE2DADD)]
        public STUGUID StateScriptComponent;

        [STUField(0xB48F1D22, "m_name")]
        public STUGUID Name;

        [STUField(0xD75586C0)]
        public LootboxBundle[] Bundles;

        [STUField(0xE02BEE24)]
        public STUGUID Virtual0C3;

        [STUField(0x3DFAC8CA)]
        public uint Unknown1;  // 0

        [STUField(0x7AB4E3F8)]
        public STUEnumEventID Event;

        [STUField(0xFFE7768F)]
        public STUGUID Effect3;

        [STUField(0xB2F9D222)]
        public STUGUID Effect4;

        [STUField(0x9B180535)]
        public STUGUID Material2;

        [STUField(0xFA2D81E7)]
        public byte Unknown2;

        [STUField(0x45C33D76)]
        public byte Unknown3;

        [STU(0x819B4F6D)]
        public class LootboxBundle : STUInstance {
            [STUField(0x90EB924A)]
            public STUGUID String;

            [STUField(0x87EACF5F)]
            public STUGUID Image;
        }

        public string EventNameNormal => ItemEvents.GetInstance().GetEventNormal((ulong)Event);
        public string EventName => ItemEvents.GetInstance().GetEvent((ulong)Event);
    }
}
