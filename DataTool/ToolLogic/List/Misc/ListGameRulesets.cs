using System.Collections.Generic;
using DataTool.DataModels.GameModes;
using DataTool.Flag;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-game-rulesets", Description = "List game rulesets", CustomFlags = typeof(ListFlags))]
    public class ListGameRulesets : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(data, flags);
                    return;
                }
        }

        private Dictionary<teResourceGUID, GameRuleset> GetData() {
            var @return = new Dictionary<teResourceGUID, GameRuleset>();

            foreach (teResourceGUID key in TrackedFiles[0xC0]) {
                var ruleset = new GameRuleset(key);
                @return[key] = ruleset.GUID == 0 ? null : ruleset;
            }

            return @return;
        }
    }
}