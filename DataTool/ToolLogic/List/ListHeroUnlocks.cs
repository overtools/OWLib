using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.JSON;
using Newtonsoft.Json;
using OWLib;
using STULib;
using STULib.Types;
using STULib.Types.STUUnlock;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;

// ReSharper disable SuspiciousTypeConversion.Global

namespace DataTool.ToolLogic.List {
    [Tool("list-unlocks", Description = "List hero unlocks", TrackTypes = new ushort[] { 0x75 }, CustomFlags = typeof(ListFlags))]
    public class ListHeroUnlocks : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class Info {
            public string Name;
            public string Rarity;
            public string Type;
            [JsonIgnore]
            public string Description;
            [JsonIgnore]
            public Cosmetic Unlock;
            [JsonConverter(typeof(GUIDConverter))]
            public ulong GUID;

            public Info(string Name, string Rarity, string Type, string Description, Cosmetic Unlock, ulong GUID) {
                this.Name = Name;
                this.Rarity = Rarity;
                this.Type = Type;
                this.Description = Description;
                this.Unlock = Unlock;
                this.GUID = GUID;
            }
        }

        public void Parse(ICLIFlags toolFlags) {
            Dictionary<string, Dictionary<string, HashSet<Info>>> unlocks = GatherUnlocks(false);

            if (toolFlags is ListFlags flags) {
                if (flags.JSON) {
                    ParseJSON(unlocks, flags);
                    return;
                }
            }

            foreach (KeyValuePair<string, Dictionary<string, HashSet<Info>>> heroPair in unlocks) {
                if (heroPair.Value?.Count == 0 || 
                    heroPair.Value.Any(it => it.Value?.Any(itt => itt?.Name != null) == false)) {
                    continue;
                }

                Log("Unlocks for {0}", heroPair.Key);

                foreach (KeyValuePair<string, HashSet<Info>> unlockPair in heroPair.Value) {
                    if (unlockPair.Value?.Count == 0) {
                        continue;
                    }

                    Log("\t{0} Unlocks", unlockPair.Key);

                    if (unlockPair.Value != null) {
                        foreach (Info unlock in unlockPair.Value) {
                            if (unlock?.Name == null) {
                                continue;
                            }

                            Log("\t\t{0} ({1} {2})", unlock.Name, unlock.Rarity, unlock.Type);
                        }
                    }
                }

                Log();
            }
        }

        public Dictionary<string, Dictionary<string, HashSet<Info>>> GatherUnlocks(bool onlyAI) {
            Dictionary<string, Dictionary<string, HashSet<Info>>> @return = new Dictionary<string, Dictionary<string, HashSet<Info>>>();
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

                    Dictionary<string, HashSet<Info>> unlocks = GatherUnlocks(hero.LootboxUnlocks, false);
                    if (unlocks == null) {
                        continue;
                    }

                    @return[name] = unlocks;
                }
            }
            return @return;
        }

        public Dictionary<string, HashSet<Info>> GatherUnlocks(ulong GUID, bool onlyAI) {
            Dictionary<string, HashSet<Info>> @return = new Dictionary<string, HashSet<Info>>();

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
                            @return["Standard"] = new HashSet<Info>();
                        }

                        foreach (Info info in GatherUnlocks(defaultUnlocks.Unlocks.Select(it => (ulong) it))) {
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
                            @return[eventKey] = new HashSet<Info>();
                        }

                        foreach (Info info in GatherUnlocks(eventUnlocks.Data.Unlocks.Select(it => (ulong) it))) {
                            @return[eventKey].Add(info);
                        }
                    }
                }
            }

            return @return;
        }

        public HashSet<Info> GatherUnlocks(IEnumerable<ulong> GUIDs) {
            HashSet<Info> @return = new HashSet<Info>();
            foreach (ulong GUID in GUIDs) {
                Info unlock = GatherUnlock(GUID);
                if (unlock == null) {
                    continue;
                }
                @return.Add(unlock);
            }
            return @return;
        }

        public Info GatherUnlock(ulong GUID) {
            using (Stream stream = OpenFile(GUID)) {
                if (stream == null) {
                    return null;
                }

                ISTU stu = ISTU.NewInstance(stream, BuildVersion);

                Cosmetic unlock = stu.Instances.OfType<Cosmetic>().First();
                if (unlock == null) {
                    return null;
                }

                string name = GetString(unlock.CosmeticName);

                if (unlock is Currency) {
                    name = $"{(unlock as Currency).Amount} Credits";
                } else if (unlock is Portrait) {
                    Portrait portrait = unlock as Portrait;
                    name = $"{portrait.Tier} - {portrait.Level} - {portrait.Star} star";
                }

                if (name == null) {
                    name = $"{OWLib.GUID.LongKey(GUID):X12}";
                }
                return new Info(name, unlock.CosmeticRarity.ToString(), unlock.GetType().Name, GetString(unlock.CosmeticDescription), unlock, GUID);
            }
        }
    }
}
