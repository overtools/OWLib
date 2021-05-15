using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels.Hero;
using TankLib.STU.Types;

namespace DataTool.Helper {
    public static class Helpers {
        public static Dictionary<ulong, STUHero> GetHeroes() {
            var @return = new Dictionary<ulong, STUHero>();
            foreach (ulong key in Program.TrackedFiles[0x75]) {
                var hero = STUHelper.GetInstance<STUHero>(key);
                if (hero == null) continue;
                @return[key] = hero;
            }

            return @return;
        }

        public static Dictionary<ulong, string> GetHeroNamesMapping(Dictionary<ulong, STUHero> heroes = null) {
            if (heroes == null)
                heroes = GetHeroes();

            return heroes.ToDictionary(x => x.Key, x => Hero.GetCleanName(x.Value)?.ToLowerInvariant());
        }
    }
}