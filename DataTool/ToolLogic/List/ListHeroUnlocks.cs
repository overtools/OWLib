using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;

namespace DataTool.ToolLogic.List {
    [Tool("list-unlocks", Description = "List hero unlocks", CustomFlags = typeof(ListFlags))]
    public class ListHeroUnlocks : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var heroUnlocks = GetUnlocks();
            var flags = (ListFlags) toolFlags;

            if (flags.JSON) {
                if (flags.Flatten) {
                    var @out = heroUnlocks.ToDictionary(x => x.Key, x => x.Value.IterateUnlocks());
                    OutputJSON(@out, flags);
                } else {
                    OutputJSON(heroUnlocks, flags);
                }

                return;
            }

            foreach (var (heroName, unlocks) in heroUnlocks) {
                Log("Unlocks for {0}", heroName);

                if (unlocks.LevelUnlocks != null) {
                    foreach (LevelUnlocks levelUnlocks in unlocks.LevelUnlocks) {
                        DisplayUnlocks("Default", levelUnlocks.Unlocks);
                    }
                }

                if (unlocks.OtherUnlocks != null) {
                    var owlUnlocks = unlocks.OtherUnlocks.Where(u => u.STU.m_0B1BA7C1 != null).ToArray();
                    var otherUnlocks = unlocks.OtherUnlocks.Where(u => u.STU.m_0B1BA7C1 == null).ToArray();
                    DisplayUnlocks("Other", otherUnlocks);
                    DisplayUnlocks("OWL", owlUnlocks);
                }

                if (unlocks.LootBoxesUnlocks != null) {
                    foreach (LootBoxUnlocks lootBoxUnlocks in unlocks.LootBoxesUnlocks) {
                        string boxName = LootBox.GetName(lootBoxUnlocks.LootBoxType);

                        DisplayUnlocks(boxName, lootBoxUnlocks.Unlocks);
                    }
                }

                Log(); // New line
            }
        }

        public static void DisplayUnlocks(string category, Unlock[] unlocks, string start = "") {
            if (unlocks == null || unlocks.Length == 0) return;
            Log($"{start}\t{category} Unlocks");

            foreach (Unlock unlock in unlocks) {
                Log($"{start}\t\t{unlock.GetName()} ({unlock.Rarity} {unlock.Type})");
                if (!string.IsNullOrEmpty(unlock.Description)) {
                    Log($"{start}\t\t\t{unlock.Description}");
                }

                if (!string.IsNullOrEmpty(unlock.AvailableIn)) {
                    Log($"{start}\t\t\t{unlock.AvailableIn}");
                }

                if (unlock.STU.m_0B1BA7C1 != null) {
                    TeamDefinition teamDef = new TeamDefinition(unlock.STU.m_0B1BA7C1);
                    Log($"{start}\t\t\tTeam: {teamDef.FullName}");
                }
            }
        }

        public static Dictionary<string, ProgressionUnlocks> GetUnlocks() {
            var @return = new Dictionary<string, ProgressionUnlocks>();
            var heroes = Helpers.GetHeroes();
            foreach (var (heroGuid, hero) in heroes) {
                if (!hero.IsHero || string.IsNullOrEmpty(hero.Name)) {
                    continue;
                }

                @return[hero.Name] = new ProgressionUnlocks(hero.STU);
            }

            return @return;
        }
    }
}
