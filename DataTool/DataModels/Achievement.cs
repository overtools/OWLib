using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    [JsonObject(MemberSerialization.OptOut)]
    public class Achievement {
        public string Name;
        public string Description;
        public Unlock Reward;

        [JsonConverter(typeof(StringEnumConverter))]
        public Enum_8E40F295 Trophy;
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Enum_116F9601 Category;

        public int GamerScore;

        public Achievement(STUAchievement achievement) {
            Name = GetString(achievement.m_name);
            Description = GetString(achievement.m_description);

            Trophy = achievement.m_trophy;
            Category = achievement.m_category;
            GamerScore = achievement.m_gamerScore;

            if (achievement.m_unlock != 0) {
                Reward = new Unlock(achievement.m_unlock);
            }
        }
    }
}