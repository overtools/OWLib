using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels.Hero;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.Helper;

public static class Helpers {
    /// <summary>
    /// Returns dictionary of all Heroes by GUID using the <see cref="Hero"/> data model.
    /// </summary>
    public static Dictionary<teResourceGUID, Hero> GetHeroes() {
        var @return = new Dictionary<teResourceGUID, Hero>();
        foreach (teResourceGUID key in Program.TrackedFiles[0x75]) {
            var hero = STUHelper.GetInstance<STUHero>(key);
            if (hero == null) continue;
            @return[key] = new Hero(hero, key);
        }

        return @return;
    }

    /// <summary>
    /// Returns a mapping of hero names to their GUIDs in lowercase.
    /// </summary>
    /// <param name="heroes">optional heroes dict if you already have one from <see cref="GetHeroes"/></param>
    public static Dictionary<teResourceGUID, string> GetHeroNamesMapping(Dictionary<teResourceGUID, Hero> heroes = null) {
        heroes ??= GetHeroes();
        return heroes.ToDictionary(x => x.Key, x => x.Value.Name?.ToLowerInvariant());
    }
}