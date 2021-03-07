using DataTool.DataModels.GameModes;
using DataTool.Flag;
using TankLib.STU.Types;
using System.Collections.Generic;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-game-ruleset-schemas", Description = "List game rulesets schemas", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListGameRulesetSchemas : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSONAlt(data, flags);
                    return;
                }
        }

        public static Dictionary<teResourceGUID, GameRulesetSchema> GetData() {
            var gameRulesets = new Dictionary<teResourceGUID, GameRulesetSchema>();

            foreach (teResourceGUID key in TrackedFiles[0xC6]) {
                var rulesetSchema = GetInstance<STUGameRulesetSchema>(key);
                if (rulesetSchema == null) continue;

                var ruleset = new GameRulesetSchema(rulesetSchema, key);
                gameRulesets[key] = ruleset;
            }

            return gameRulesets;
        }
    }
}
