using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels.GameModes;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-heroes-rulesets", Description = "List heroes rulesets", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListHeroesRulesets : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSONAlt(data, flags);
                    return;
                }
        }

        public Dictionary<teResourceGUID, HeroRulesets> GetData() {
            var heroes = new Dictionary<teResourceGUID, HeroRulesets>();

            foreach (teResourceGUID key in TrackedFiles[0x75]) {
                var hero = new Hero(key);
                if (hero.GUID == 0)
                    continue;

                heroes[key] = new HeroRulesets {
                    GUID = key,
                    Name = hero.Name,
                    RulesetSchemas = hero.STU.m_gameRulesetSchemas?.Select(x => new GameRulesetSchema(x)).ToArray()
                };
            }

            return heroes;
        }

        public class HeroRulesets {
            public teResourceGUID GUID;
            public string Name;
            public GameRulesetSchema[] RulesetSchemas;
        }
    }
}
