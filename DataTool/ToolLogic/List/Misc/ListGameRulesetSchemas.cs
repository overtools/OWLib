using DataTool.DataModels.GameModes;
using DataTool.Flag;
using System.Collections.Generic;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;

namespace DataTool.ToolLogic.List.Misc;

[Tool("list-game-ruleset-schemas", Description = "List game rulesets schemas", CustomFlags = typeof(ListFlags), IsSensitive = true)]
public class ListGameRulesetSchemas : JSONTool, ITool {
    public void Parse(ICLIFlags toolFlags) {
        var data = GetData();
        var flags = (ListFlags) toolFlags;
        OutputJSON(data, flags);
    }

    public static Dictionary<teResourceGUID, GameRulesetSchema> GetData() {
        var gameRulesets = new Dictionary<teResourceGUID, GameRulesetSchema>();

        foreach (teResourceGUID key in TrackedFiles[0xC6]) {
            var rulesetSchema = GameRulesetSchema.Load(key);
            if (rulesetSchema == null) continue;
            gameRulesets[key] = rulesetSchema;
        }

        return gameRulesets;
    }
}