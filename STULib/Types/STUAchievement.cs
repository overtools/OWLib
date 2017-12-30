using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x0CC07049, "STUAchievement")]
    public class STUAchievement : STUInstance {
        [STUField(0xB48F1D22, "m_name")]
        public STUGUID Name;

        [STUField(0xCA7E6EDC)]
        public STUGUID Description;

        [STUField(0x290B2ADF)]
        public STUGUID UnlockAnnouncementMessage;

        [STUField(0x245A3F6D)]
        public STUGUID UnlockAnnouncementImage;

        [STUField(0xF5087894)]
        public STUGUID Reward;

        [STUField(0x544A6A4F)]
        public STUGUID Image;

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
    public class STU_C1A2DB26 : STUInstance {  // this will get their own home when I find a file whith it used
        [STUField(0xA20DCD80)]
        public ulong m_A20DCD80;

        [STUField(0x0619C597, "m_type")]
        public Enums.STUEnum_9EAD8C06 Type;

        [STUField(0x967A138B)]
        public Enums.STUEnum_AB6CE3D1 m_967A138B;
    }
}
namespace STULib.Types.Enums {  // these will get their own home when I find a file whith them used
    [STUEnum(0x9EAD8C06)]
    public enum STUEnum_9EAD8C06 : uint {
    }

    [STUEnum(0xAB6CE3D1)]
    public enum STUEnum_AB6CE3D1 : uint {
    }
}
