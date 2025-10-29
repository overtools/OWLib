#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels.Hero;
using TankLib;

namespace DataTool.Helper;

public static class Helpers {
    private static Dictionary<teResourceGUID, HeroVM>? _heroesCache;
    private static Dictionary<teResourceGUID, LoadoutVM>? _loadoutsCache;

    /// <summary>
    /// Returns dictionary of all Heroes by GUID using the <see cref="HeroVM"/> data model.
    /// </summary>
    public static Dictionary<teResourceGUID, Hero> GetHeroes() {
        if (_heroesCache != null) {
            return _heroesCache;
        }

        var @return = new Dictionary<teResourceGUID, Hero>();
        foreach (teResourceGUID key in Program.TrackedFiles[0x75]) {
            var hero = HeroVM.Load(key);
            if (hero == null) continue;
            @return[key] = hero;
        }

        _heroesCache = @return;
        return @return;
    }

    /// <summary>
    /// Returns dictionary of all Loadouts by GUID using the <see cref="LoadoutVM"/> data model.
    /// </summary>
    public static Dictionary<teResourceGUID, Loadout> GetLoadouts() {
        if (_loadoutsCache != null) {
            return _loadoutsCache;
        }

        var @return = new Dictionary<teResourceGUID, Loadout>();
        foreach (teResourceGUID key in Program.TrackedFiles[0x9E]) {
            var loadout = LoadoutVM.Load(key);
            if (loadout == null) continue;
            @return[key] = loadout;
        }

        _loadoutsCache = @return;
        return @return;
    }

    /// <summary>Returns a Loadout by its GUID</summary>
    public static LoadoutVM? GetLoadoutById(ulong guid) {
        var loadouts = GetLoadouts();
        return loadouts.GetValueOrDefault((teResourceGUID) guid);
    }

    /// <summary>Returns a Hero by its GUID</summary>
    public static HeroVM? GetHeroById(ulong guid) {
        var heroes = GetHeroes();
        return heroes.GetValueOrDefault((teResourceGUID) guid);
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