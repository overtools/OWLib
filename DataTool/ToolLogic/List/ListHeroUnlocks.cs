using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using OWLib;
using STULib;
using STULib.Types;
using STULib.Types.STUUnlock;
using static DataTool.Helper.CascIO;
using static DataTool.Program;
using static DataTool.Helper.Logger;

// ReSharper disable SuspiciousTypeConversion.Global

namespace DataTool.ToolLogic.List {
    [Tool("list-unlocks", Description = "List hero unlocks", TrackTypes = new ushort[] { 0x75 })]
    public class ListHeroUnlocks : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public class Info {
            public string Name;
            public string Rarity;
            public string Type;
            public string Description;
            public Cosmetic Unlock;
            public ulong GUID;

            public Info(string Name, string Rarity, string Type, string Description, Cosmetic Unlock, ulong GUID) {
                this.Name = Name;
                this.Rarity = Rarity;
                this.Type = Type;
                this.Description = Description;
                this.Unlock = Unlock;
                this.GUID = GUID;
            }
        };

        public void Parse(ICLIFlags toolFlags) {
            Dictionary<string, Dictionary<string, HashSet<Info>>> unlocks = GatherUnlocks();
            foreach (KeyValuePair<string, Dictionary<string, HashSet<Info>>> heroPair in unlocks) {
                if (heroPair.Value?.Count == 0 || heroPair.Value.Any(it => it.Value?.Any(itt => itt?.Name != null) == false) == false) {
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

        public Dictionary<string, Dictionary<string, HashSet<Info>>> GatherUnlocks() {
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

                    @return[name] = GatherUnlocks(hero.LootboxUnlocks);
                }
            }
            return @return;
        }

        public Dictionary<string, HashSet<Info>> GatherUnlocks(ulong GUID) {
            Dictionary<string, HashSet<Info>> @return = new Dictionary<string, HashSet<Info>>();

            using (Stream stream = OpenFile(GUID)) {
                if (stream == null) {
                    return @return;
                }

                ISTU stu = ISTU.NewInstance(stream, BuildVersion);

                STUHeroUnlocks unlocks = stu.Instances.OfType<STUHeroUnlocks>().First();
                if (unlocks == null) {
                    return @return;
                }

                @return["Default"] = GatherUnlocks(unlocks.SystemUnlocks?.Unlocks?.Cast<ulong>());

                foreach (STUHeroUnlocks.UnlockInfo defaultUnlocks in unlocks.Unlocks) {
                    if (defaultUnlocks?.Unlocks == null) {
                        continue;
                    }
                    if (!@return.ContainsKey("Standard")) {
                        @return["Standard"] = new HashSet<Info>();
                    }

                    foreach (Info info in GatherUnlocks(defaultUnlocks.Unlocks.Cast<ulong>())) {
                        @return["Standard"].Add(info);
                    }
                }

                foreach (STUHeroUnlocks.EventUnlockInfo eventUnlocks in unlocks.LootboxUnlocks) {
                    if (eventUnlocks?.Data?.Unlocks == null) {
                        continue;
                    }
                    if (!@return.ContainsKey("Standard")) {
                        @return[$"Event/{ItemEvents.GetInstance().EventsNormal[eventUnlocks.EventID]}"] = new HashSet<Info>();
                    }

                    foreach (Info info in GatherUnlocks(eventUnlocks.Data.Unlocks.Cast<ulong>())) {
                        @return[$"Event/{ItemEvents.GetInstance().EventsNormal[eventUnlocks.EventID]}"].Add(info);
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
                    name = $"{portrait.Tier} tier - {portrait.Bracket} - {portrait.Star} star";
                }

                return name == null ? null : new Info(name, unlock.CosmeticRarity.ToString(), unlock.GetType().Name, GetString(unlock.CosmeticDescription), unlock, GUID);
            }
        }
    }
}
