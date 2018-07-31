using System;
using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-achievements", Description = "List achievements", TrackTypes = new ushort[] {0x68}, CustomFlags = typeof(ListFlags))]
    public class ListAchievements : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            List<Achievement> achievements = GetAchievements();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(achievements, flags);
                    return;
                }

            foreach (Achievement achievement in achievements) {
                var iD = new IndentHelper();
                
                Log($"{achievement.Name}");
                Log($"{iD+1}Description: {achievement.Description}");
                
                if (achievement.Reward != null)
                    Log($"{iD+1}Reward: {achievement.Reward.Name} ({achievement.Reward.Rarity} {achievement.Reward.Type})");

                Log();
            }
        }

        public List<Achievement> GetAchievements() {
            List<Achievement> achievements = new List<Achievement>();

            foreach (ulong key in TrackedFiles[0x68]) {
                STUAchievement achievement = GetInstance<STUAchievement>(key);
                if (achievement == null) continue;
                
                Achievement model = new Achievement(achievement);
                achievements.Add(model);
            }

            return achievements;
        }
    }
}
