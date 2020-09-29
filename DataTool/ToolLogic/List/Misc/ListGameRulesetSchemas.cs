using DataTool.DataModels.GameModes;
using DataTool.Flag;
using TankLib.STU.Types;
using System.Collections.Generic;
using DataTool.JSON;
using static DataTool.Program;
using static DataTool.Helper.Logger;
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

            Log("Ruleset Schemas:");
            foreach (var rulesetGroup in data) {
                Log($"\t{rulesetGroup.Name}");
                foreach (var entry in rulesetGroup.Entries) {
                    if (entry.Name == null) continue;
                    Log($"\t\t{entry.Name}");
                }
                Log();
            }
        }

        private static List<GameRulesetSchema> GetData() {
            var gameRulesets = new List<GameRulesetSchema>();

            foreach (var key in TrackedFiles[0xC6]) {
                var rulesetSchema = GetInstance<STUGameRulesetSchema>(key);
                if (rulesetSchema == null) continue;

                var ruleset = new GameRulesetSchema(rulesetSchema, key);
                gameRulesets.Add(ruleset);
            }

            return gameRulesets;
        }
    }
}