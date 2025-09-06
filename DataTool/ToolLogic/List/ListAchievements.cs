using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using static DataTool.Program;

namespace DataTool.ToolLogic.List;

[Tool("list-achievements", Description = "List achievements", CustomFlags = typeof(ListFlags))]
public class ListAchievements : JSONTool, ITool {
    public void Parse(ICLIFlags toolFlags) {
        var flags = (ListFlags) toolFlags;
        var data = GetData();

        if (flags.JSON) {
            OutputJSON(data, flags);
            return;
        }

        foreach (var achievement in data) {
            var iD = new IndentHelper();

            Log($"{achievement.Name}");
            if (!flags.Simplify) {
                Log($"{iD + 1}Description: {achievement.Description}");

                if (achievement.Reward != null)
                    Log($"{iD + 1}Reward: {achievement.Reward.Name} ({achievement.Reward.Rarity} {achievement.Reward.Type})");

                Log();
            }
        }
    }

    private static List<Achievement> GetData() {
        var achievements = new List<Achievement>();

        foreach (ulong key in TrackedFiles[0x68]) {
            var achievement = Achievement.Load(key);
            if (achievement == null) continue;
            achievements.Add(achievement);
        }

        return achievements;
    }
}