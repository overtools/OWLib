using System.Runtime.Serialization;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    [DataContract]
    public class Achievement {
        [DataMember]
        public string Name;
        
        [DataMember]
        public string Description;
        
        [DataMember]
        public Unlock Reward;
        
        [DataMember]
        public Enum_8E40F295 Trophy;
        
        [DataMember]
        public Enum_116F9601 Category;

        [DataMember]
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