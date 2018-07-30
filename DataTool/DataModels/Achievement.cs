using Newtonsoft.Json;
using TankLib.STU.Types;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    [JsonObject(MemberSerialization.OptOut)]
    public class Achievement {
        public string Name;
        public string Description;
        public Unlock Reward;

        public Achievement(STUAchievement achievement) {
            Name = GetString(achievement.m_name);
            Description = GetString(achievement.m_description);

            if (achievement.m_unlock != 0) {
                Reward = new Unlock(achievement.m_unlock);
            }
        }
    }
}