using STULib.Types.Generic;

namespace STULib.Types {
    [STU(0x0CC07049, "STUAchievement")]
    public class STUAchievement : Common.STUInstance {
        [STUField(0xB48F1D22, "m_name")]
        public Common.STUGUID Name;

        [STUField(0xCA7E6EDC, "m_description")]
        public Common.STUGUID Description;

        [STUField(0x290B2ADF)]
        public Common.STUGUID UnlockAnnouncementMessage;

        [STUField(0x245A3F6D)]
        public Common.STUGUID UnlockAnnouncementImage;

        [STUField(0xF5087894)]
        public Common.STUGUID Reward;

        [STUField(0x544A6A4F)]
        public Common.STUGUID Image;

        [STUField(0x4E291DCC)]
        public string InternalName;

        [STUField(0x4FF98D41, EmbeddedInstance = true)]
        public STU_C1A2DB26 m_4FF98D41;  // todo: where used

        [STUField(0x2C54AEAF, "m_category")]
        public Enums.STUEnumAchievementGroup Category;

        [STUField(0x07DD813E, "m_value")]
        public int Value;  // todo: this is currently interchangeable with XboxGamerscore

        [STUField(0x628D48CC)]
        public int XboxGamerscore;  // todo: this is currently interchangeable with Value

        [STUField(0x59D52DA5)]
        public Enums.STUEnumPS4Trophy PS4Trophy;

        [STUField(0x5351832E)]
        public int ID;
    }

    [STU(0xC1A2DB26)]
    public class STU_C1A2DB26 : Common.STUInstance {
        [STUField(0xA20DCD80)]
        public ulong m_A20DCD80;

        [STUField(0x0619C597, "m_type")]
        public STUEnum_9EAD8C06 Type;

        [STUField(0x967A138B)]
        public STUEnum_AB6CE3D1 m_967A138B;
    }

    // these will get their own home when I find a file whith them used
    [STUEnum(0x9EAD8C06)]
    public enum STUEnum_9EAD8C06 : uint {
    }

    [STUEnum(0xAB6CE3D1)]
    public enum STUEnum_AB6CE3D1 : uint {
    }
}
