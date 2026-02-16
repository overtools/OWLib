using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;

namespace DataTool.ToolLogic.List;

[Tool("list-challenges", Description = "List challenges", CustomFlags = typeof(ListFlags))]
public class ListChallenges : JSONTool, ITool {
    public void Parse(ICLIFlags toolFlags) {
        var flags = (ListFlags) toolFlags;
        var data = GetData();

        if (flags.JSON) {
            OutputJSON(data, flags);
            return;
        }

        var indentLevel = new IndentHelper();
        foreach (var (key, challenge) in data) {
            Log($"{indentLevel}{challenge.Name}:");
            if (!flags.Simplify) {
                if (challenge.Description != null) {
                    Log($"{indentLevel + 1}Description: {challenge.Description}");
                }

                if (challenge.Hero != null) {
                    Log($"{indentLevel + 1}Hero: {challenge.Hero?.Value ?? "Unknown"}");
                }

                if (challenge.RequiredUnlock != null) {
                    Log($"{indentLevel + 1}Required Unlock: {challenge.RequiredUnlock.GetFormattedName()}");
                }

                if (challenge.Rewards?.Length > 0) {
                    Log($"{indentLevel + 1}Rewards:");
                    foreach (var reward in challenge.Rewards) {
                        Log($"{indentLevel + 2}{reward?.GetFormattedName()}");
                    }
                }

                Log();
            }
        }
    }

    public static Dictionary<teResourceGUID, Challenge> GetData() {
        var map = new Dictionary<teResourceGUID, Challenge>();

        foreach (teResourceGUID guid in Program.TrackedFiles[0x157]) {
            var challenge = Challenge.Load(guid);
            if (challenge == null) continue;
            map.Add(guid, challenge);
        }

        return map;
    }
}