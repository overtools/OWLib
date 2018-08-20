using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-unlocks", Description = "List hero unlocks", TrackTypes = new ushort[] { 0x75 }, CustomFlags = typeof(ListFlags))]
    public class ListHeroUnlocks : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            Dictionary<string, ProgressionUnlocks> unlocks = GetUnlocks();

            if (toolFlags is ListFlags flags) {
                if (flags.JSON) {
                    ParseJSON(unlocks, flags);
                    return;
                }
            }

            foreach (KeyValuePair<string, ProgressionUnlocks> heroPair in unlocks) {
                Log("Unlocks for {0}", heroPair.Key);

                if (heroPair.Value.LevelUnlocks != null) {
                    foreach (LevelUnlocks levelUnlocks in heroPair.Value.LevelUnlocks) {
                        DisplayUnlocks("Default", levelUnlocks.Unlocks);
                    }
                }

                if (heroPair.Value.OtherUnlocks != null) {
                    var owlUnlocks = heroPair.Value.OtherUnlocks.Where(u => u.STU.m_0B1BA7C1 != null).ToArray();
                    var otherUnlocks = heroPair.Value.OtherUnlocks.Where(u => u.STU.m_0B1BA7C1 == null).ToArray();
                    DisplayUnlocks("Other", otherUnlocks);
                    DisplayUnlocks("OWL", owlUnlocks);
                }

                if (heroPair.Value.LootBoxesUnlocks != null) {
                    foreach (LootBoxUnlocks lootBoxUnlocks in heroPair.Value.LootBoxesUnlocks) {
                        string boxName = LootBox.GetName(lootBoxUnlocks.LootBoxType);
                        
                        DisplayUnlocks(boxName, lootBoxUnlocks.Unlocks);
                    }
                }
            }
        }

        public static void DisplayUnlocks(string category, Unlock[] unlocks, string start="") {
            if (unlocks == null || unlocks.Length == 0) return;
            Log($"{start}\t{category} Unlocks");

            foreach (Unlock unlock in unlocks) {
                Log($"{start}\t\t{unlock.GetName()} ({unlock.Rarity} {unlock.Type})");
                if (unlock.Description != null) {
                    Log($"{start}\t\t\t{unlock.Description}");
                }
            }
        }

        public static Dictionary<string, ProgressionUnlocks> GetUnlocks() {
            Dictionary<string, ProgressionUnlocks> @return = new Dictionary<string, ProgressionUnlocks>();
            foreach (ulong key in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(key);
                if (hero == null) continue;

                string name = GetString(hero.m_0EDCE350);
                if (name == null) continue;

                ProgressionUnlocks unlocks = new ProgressionUnlocks(hero);

                @return[name] = unlocks;
            }

            return @return;
        }
    }
}
