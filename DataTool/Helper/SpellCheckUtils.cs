using DataTool.DataModels;
using DataTool.DataModels.Hero;
using DataTool.ToolLogic.List;
using System.Collections.Generic;
using System.Reflection;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Program;
using static TankLib.Helpers.Logger;

namespace DataTool.Helper {
    public static class SpellCheckUtils {
        public static void SpellCheckString(string str, SymSpell checker) {
            if (str == null || str == "*")
                return;

            var correctedStr = checker.Lookup(str.ToLower(), SymSpell.Verbosity.Closest);
            if (correctedStr.Count == 0 || correctedStr[0].term == str)
                return;

            Warn("SpellCheck", $"Did you mean {correctedStr[0].term}?");
        }

        public static void SpellCheckQuery(Dictionary<string, Dictionary<string, ParsedArg>> pTypes, SymSpell symSpell) {
            foreach (var type in pTypes) {
                SpellCheckString(type.Key, symSpell);
            }
        }

        public static void SpellCheckMapName(Dictionary<string, Dictionary<string, ParsedArg>> pTypes, SymSpell symSpell) {
            foreach (var map_name in pTypes) {
                SpellCheckString((map_name.Key.Contains(':') ? map_name.Key.Split(':')[0] : map_name.Key), symSpell); //for MapName:GUID case
            }
        }

        public static void FillHeroSpellDict(SymSpell symSpell) {
            var heroes = new Dictionary<teResourceGUID, Hero>();

            foreach (teResourceGUID key in TrackedFiles[0x75]) {
                var hero = new Hero(key);
                if (hero.GUID == 0)
                    continue;

                heroes[key] = hero;
            }

            foreach (var (_, hero) in heroes) {
                if (hero.Name != null)
                    symSpell.CreateDictionaryEntry(hero.Name.ToLower(), 1);
            }
        }

        public static void FillMapSpellDict(SymSpell symSpell) {
            foreach (ulong key in TrackedFiles[0x9F]) {
                STUMapHeader map = GetInstance<STUMapHeader>(key);
                if (map == null) continue;
                MapHeader mapInfo = ListMaps.GetMap(key);
                symSpell.CreateDictionaryEntry(mapInfo.GetName().ToLower(), 1);
            }
        }

        public static void FillToolSpellDict(SymSpell symSpell) {
            foreach (var type in GetTools(false)) {
                var attribute = type.GetCustomAttribute<ToolAttribute>();
                symSpell.CreateDictionaryEntry(attribute.Keyword.ToLower(), 1);
            }
        }

    }
}
