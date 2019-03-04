using System;
using DataTool.DataModels.GameModes;
using DataTool.Flag;
using TankLib.STU.Types;
using System.Collections.Generic;
using DataTool.JSON;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-game-rulesets", Description = "List game rulesets", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListGameParams : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            var rulesetData = GetGameRulesets();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSONAlt(rulesetData, flags);
                    return;
                }

            Log("Ruleset Schemas:");
            foreach (var rulesetGroup in rulesetData) {
                Log($"\t{rulesetGroup.Name}");
                foreach (var entry in rulesetGroup.Entries) {
                    if (entry.Name == null) continue;
                    Log($"\t\t{entry.Name}");
                }
                Log();
            }
        }

        private static List<GameRulesetSchema> GetGameRulesets() {
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