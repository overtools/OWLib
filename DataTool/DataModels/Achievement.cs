using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels {
    public class Achievement {
        public teResourceGUID GUID { get; set; }
        public string Name { get; set; }
        public string AchievementName { get; set; }
        public string Description { get; set; }
        public UnlockLite Reward { get; set; }
        //public Enum_8E40F295 Trophy { get; set; }
        //public Enum_116F9601 Category { get; set; }
        public int GamerScore { get; set; }

        public Achievement(ulong key) {
            STUAchievement stu = GetInstance<STUAchievement>(key);
            Init(stu, key);
        }

        public Achievement(STUAchievement stu, ulong key = default) {
            Init(stu, key);
        }

        public void Init(STUAchievement achievement, ulong key = default) {
            if (achievement == null) return;

            GUID = (teResourceGUID) key;
            Name = GetString(achievement.m_name);
            AchievementName = achievement.m_4E291DCC?.Value;
            Description = GetString(achievement.m_description);

            //Trophy = achievement.m_trophy;
            //Category = achievement.m_category;
            GamerScore = achievement.m_628D48CC;

            if (achievement.m_unlock != 0) {
                Reward = new Unlock(achievement.m_unlock).ToLiteUnlock();
            }
        }
    }
}