#nullable enable
using System;
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
    private static Dictionary<teResourceGUID, string> GetHeroNamesMapping(Dictionary<teResourceGUID, Hero>? heroes=null) {
        heroes ??= GetHeroes();
        return heroes
            .Where(x => x.Value.Name != null)
            .ToDictionary(x => x.Key, x => x.Value.Name!);
    }

    public static IgnoreCaseDict<string> GetHeroNameLocaleOverrides(Dictionary<teResourceGUID, Hero>? heroes=null) {
        var namesForThisLocale = GetHeroNamesMapping(heroes);

        var overrides = new IgnoreCaseDict<string>();
        foreach (var localizedName in IO.GetLocalizedNames(0x75)) {
            if (!namesForThisLocale.TryGetValue(localizedName.Value, out var nameForThisLocale)) {
                continue;
            }
            
            if (localizedName.Key.Equals(nameForThisLocale, StringComparison.OrdinalIgnoreCase)) {
                // identical, don't bother mapping
                continue;
            }
            if (namesForThisLocale.Values.Any(x => x.Equals(localizedName.Key, StringComparison.OrdinalIgnoreCase))) {
                // theoretically, this locale already has a hero with that name. give up
                continue;
            }
            overrides[localizedName.Key] = nameForThisLocale;
        }

        return overrides;
    }
}