using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels {
    [DataContract]
    public class Achievement {
        [DataMember]
        public teResourceGUID GUID;
        
        [DataMember]
        public string Name;
        
        [DataMember]
        public string AchievementName;
        
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
        
        public Achievement(ulong key) {
            STUAchievement stu = GetInstance<STUAchievement>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public Achievement(STUAchievement stu) {
            Init(stu);
        }

        public void Init(STUAchievement achievement, ulong key = default) {
            GUID = (teResourceGUID) key;
            Name = GetString(achievement.m_name);
            AchievementName = achievement.m_4E291DCC.Value;
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