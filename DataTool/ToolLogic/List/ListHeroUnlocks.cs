using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using OWLib;
using STULib;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;
using DataTool.DataModels;

// ReSharper disable SuspiciousTypeConversion.Global

namespace DataTool.ToolLogic.List {
    [Tool("list-unlocks", Description = "List hero unlocks", TrackTypes = new ushort[] { 0x75 }, CustomFlags = typeof(ListFlags))]
    public class ListHeroUnlocks : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            Dictionary<string, Dictionary<string, HashSet<ItemInfo>>> unlocks = GatherUnlocks(false);

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

        public Dictionary<string, Dictionary<string, HashSet<ItemInfo>>> GatherUnlocks(bool onlyAI) {
            Dictionary<string, Dictionary<string, HashSet<ItemInfo>>> @return = new Dictionary<string, Dictionary<string, HashSet<ItemInfo>>>();
            foreach (ulong key in TrackedFiles[0x75]) {
                using (Stream stream = OpenFile(key)) {
                    if (stream == null) {
                        continue;
                    }

                    ISTU stu = ISTU.NewInstance(stream, BuildVersion);
                    STUHero hero = stu.Instances.OfType<STUHero>().First();
                    if (hero == null) {
                        continue;
                    }

                    string name = GetString(hero.Name);

                    if (name == null) {
                        continue;
                    }

                    Dictionary<string, HashSet<ItemInfo>> unlocks = GatherUnlocks(hero.LootboxUnlocks, false);
                    if (unlocks == null) {
                        continue;
                    }

                    @return[name] = unlocks;
                }
            }
            return @return;
        }

        public Dictionary<string, HashSet<ItemInfo>> GatherUnlocks(ulong GUID, bool onlyAI) {
            Dictionary<string, HashSet<ItemInfo>> @return = new Dictionary<string, HashSet<ItemInfo>>();

            using (Stream stream = OpenFile(GUID)) {
                if (stream == null) {
                    return null;
                }

                ISTU stu = ISTU.NewInstance(stream, BuildVersion);

                STUHeroUnlocks unlocks = stu.Instances.OfType<STUHeroUnlocks>().First();
                if (unlocks == null) {
                    return null;
                }

                if (onlyAI && unlocks.Unlocks != null) {
                    return null;
                } if (!onlyAI && unlocks.Unlocks == null) {
                    return null;
                }

                @return["Default"] = GatherUnlocks(unlocks.SystemUnlocks?.Unlocks?.Select(it => (ulong)it));

                if (unlocks.Unlocks != null) {
                    foreach (STUHeroUnlocks.UnlockInfo defaultUnlocks in unlocks.Unlocks) {
                        if (defaultUnlocks?.Unlocks == null) {
                            continue;
                        }
                        if (!@return.ContainsKey("Standard")) {
                            @return["Standard"] = new HashSet<ItemInfo>();
                        }

                        foreach (ItemInfo info in GatherUnlocks(defaultUnlocks.Unlocks.Select(it => (ulong) it))) {
                            @return["Standard"].Add(info);
                        }
                    }
                }

                if (unlocks.LootboxUnlocks != null) {
                    foreach (STUHeroUnlocks.EventUnlockInfo eventUnlocks in unlocks.LootboxUnlocks) {
                        if (eventUnlocks?.Data?.Unlocks == null) {
                            continue;
                        }

                        string eventKey = $"Event/{ItemEvents.GetInstance().EventsNormal[(uint)eventUnlocks.Event]}";

                        if (!@return.ContainsKey(eventKey)) {
                            @return[eventKey] = new HashSet<ItemInfo>();
                        }

                        foreach (ItemInfo info in GatherUnlocks(eventUnlocks.Data.Unlocks.Select(it => (ulong) it))) {
                            @return[eventKey].Add(info);
                        }
                    }
                }
            }

            return @return;
        }

        public HashSet<ItemInfo> GatherUnlocks(IEnumerable<ulong> GUIDs) {
            HashSet<ItemInfo> @return = new HashSet<ItemInfo>();
            foreach (ulong GUID in GUIDs) {
                ItemInfo unlock = GatherUnlock(GUID);
                if (unlock == null) {
                    continue;
                }
                @return.Add(unlock);
            }
            return @return;
        }
    }
}
