using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels.GameModes;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-heroes-rulesets", Description = "List heroes rulesets", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListHeroesRulesets : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();
            var flags = (ListFlags) toolFlags;
            OutputJSON(data, flags);
        }

        public Dictionary<teResourceGUID, HeroRulesets> GetData() {
            var rulesets = new Dictionary<teResourceGUID, HeroRulesets>();

            foreach (var (heroGuid, hero) in Helpers.GetHeroes()) {
                var guid = new teResourceGUID(heroGuid);
                rulesets[guid] = new HeroRulesets {
                    GUID = guid,
                    Name = hero.Name,
                    RulesetSchemas = hero.STU.m_gameRulesetSchemas?.Select(x => new GameRulesetSchema(x)).ToArray()
                };
            }

            return rulesets;
        }

        public class HeroRulesets {
            public teResourceGUID GUID;
            public string Name;
            public GameRulesetSchema[] RulesetSchemas;
        }
    }
}
