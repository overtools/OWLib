using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

// ReSharper disable SuspiciousTypeConversion.Global

namespace DataTool.ToolLogic.List {
    [Tool("list-unlocks", Description = "List hero unlocks", TrackTypes = new ushort[] { 0x75 }, CustomFlags = typeof(ListFlags))]
    public class ListHeroUnlocks : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            Dictionary<string, Dictionary<string, HashSet<ItemInfo>>> unlocks = GetUnlocks();

            if (toolFlags is ListFlags flags) {
                if (flags.JSON) {
                    ParseJSON(unlocks, flags);
                    return;
                }
            }

            foreach (KeyValuePair<string, Dictionary<string, HashSet<ItemInfo>>> heroPair in unlocks) {
                if (heroPair.Value?.Count == 0 || 
                    heroPair.Value.Any(it => it.Value?.Any(itt => itt?.Name != null) == false)) {
                    continue;
                }

                Log("Unlocks for {0}", heroPair.Key);

                foreach (KeyValuePair<string, HashSet<ItemInfo>> unlockPair in heroPair.Value) {
                    if (unlockPair.Value?.Count == 0) {
                        continue;
                    }

                    Log("\t{0} Unlocks", unlockPair.Key);

                    if (unlockPair.Value != null) {
                        foreach (ItemInfo unlock in unlockPair.Value) {
                            if (unlock?.Name == null) {
                                continue;
                            }

                            Log("\t\t{0} ({1} {2})", unlock.Name, unlock.Rarity, unlock.Type);
                            if (unlock.Description != null) {
                                Log("\t\t\t{0}", unlock.Description);
                            }
                        }
                    }
                }

                Log();
            }
        }

        public static Dictionary<string, Dictionary<string, HashSet<ItemInfo>>> GetUnlocks() {
            Dictionary<string, Dictionary<string, HashSet<ItemInfo>>> @return = new Dictionary<string, Dictionary<string, HashSet<ItemInfo>>>();
            foreach (ulong key in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(key);
                if (hero == null) continue;

                string name = GetString(hero.Name);
                if (name == null) continue;

                Dictionary<string, HashSet<ItemInfo>> unlocks = GetUnlocksForHero(hero.LootboxUnlocks);
                if (unlocks == null) continue;

                @return[name] = unlocks;
            }

            return @return;
        }

        public static Dictionary<string, HashSet<ItemInfo>> GetUnlocksForHero(ulong guid) {
            Dictionary<string, HashSet<ItemInfo>> @return = new Dictionary<string, HashSet<ItemInfo>>();

            STUHeroUnlocks unlocks = GetInstance<STUHeroUnlocks>(guid);
            if (unlocks == null) return null;
            @return["Default"] = GatherUnlocks(unlocks.SystemUnlocks?.Unlocks?.Select(it => (ulong)it));

            if (unlocks.Unlocks != null) {
                foreach (STUHeroUnlocks.STUUnlocks defaultUnlocks in unlocks.Unlocks) {
                    if (defaultUnlocks?.Unlocks == null) continue;

                    if (!@return.ContainsKey("Standard"))
                        @return["Standard"] = new HashSet<ItemInfo>();

                    foreach (ItemInfo info in GatherUnlocks(defaultUnlocks.Unlocks.Select(it => (ulong) it))) {
                        @return["Standard"].Add(info);
                    }
                }
            }

            if (unlocks.LootboxUnlocks != null) {
                foreach (STUHeroUnlocks.STULootBoxUnlocks eventUnlocks in unlocks.LootboxUnlocks) {
                    if (eventUnlocks?.Unlocks?.Unlocks == null) continue;

                    string eventKey = $"Event/{ItemEvents.GetInstance().EventsNormal[(uint)eventUnlocks.Event]}";

                    if (!@return.ContainsKey(eventKey)) {
                        @return[eventKey] = new HashSet<ItemInfo>();
                    }

                    foreach (ItemInfo info in GatherUnlocks(eventUnlocks.Unlocks.Unlocks.Select(it => (ulong) it))) {
                        @return[eventKey].Add(info);
                    }
                }
            }

            return @return;
        }
    }
}
