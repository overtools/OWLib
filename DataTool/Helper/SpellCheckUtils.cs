using DataTool.DataModels;
using System.Collections.Generic;
using System.Reflection;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Program;
using static TankLib.Helpers.Logger;

namespace DataTool.Helper;

public static class SpellCheckUtils {
    public static void SpellCheckString(string str, SymSpell checker) {
        if (str == null || str == "*")
            return;

        var correctedStr = checker.Lookup(str.ToLower(), SymSpell.Verbosity.Closest);
        if (correctedStr.Count == 0 || correctedStr[0].term == str)
            return;

        Warn("SpellCheck", $"Did you mean {correctedStr[0].term}?");
    }

    public static void SpellCheckQuery(Dictionary<string, ParsedHero> pTypes, SymSpell symSpell) {
        foreach (var type in pTypes) {
            SpellCheckString(type.Key, symSpell);
        }
    }

    public static void SpellCheckMapName(Dictionary<string, ParsedHero> pTypes, SymSpell symSpell) {
        foreach (var map_name in pTypes) {
            SpellCheckString((map_name.Key.Contains(':') ? map_name.Key.Split(':')[0] : map_name.Key), symSpell); //for MapName:GUID case
        }
    }

    public static void FillHeroSpellDict(SymSpell symSpell) {
        var heroes = Helpers.GetHeroes();

        foreach (var (_, hero) in heroes) {
            if (hero.Name != null)
                symSpell.CreateDictionaryEntry(hero.Name.ToLower(), 1);
        }
    }

    public static void FillMapSpellDict(SymSpell symSpell) {
        foreach (ulong key in TrackedFiles[0x9F]) {
            STUMapHeader map = GetInstance<STUMapHeader>(key);
            if (map == null) continue;
            var mapInfo = MapHeader.Load(key);
            symSpell.CreateDictionaryEntry(mapInfo?.GetName().ToLower(), 1);
        }
    }

    public static void FillToolSpellDict(SymSpell symSpell) {
        foreach (var type in GetTools()) {
            var attribute = type.GetCustomAttribute<ToolAttribute>();
            if (attribute == null || attribute.IsSensitive) continue;
            symSpell.CreateDictionaryEntry(attribute.Keyword.ToLower(), 1);
        }
    }
}