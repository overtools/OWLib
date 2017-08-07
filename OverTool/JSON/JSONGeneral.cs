using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using Newtonsoft.Json;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool.JSON {
    public class JSONGeneral : IOvertool {
        public string Title => "JSON output general cosmetics";
        public char Opt => '\0';
        public string FullOpt => "json-general";
        public string Help => "output.json";
        public uint MinimumArgs => 1;
        public ushort[] Track => new ushort[1] { 0x54 };
        public bool Display => true;

        public struct JSONPkg {
            public string Name;
            public ulong Key;
            public string Description;
            public string Subline;
            public string Event;
            public ulong EventId;
            public string Rarity;
            public string Type;
        }

        public static JSONPkg GenerateJSONPkg(ulong key, Dictionary<ulong, Record> map, CASCHandler handler) {
            if (!map.ContainsKey(key)) {
                return new JSONPkg { Name = null };
            }

            STUD stud = new STUD(Util.OpenFile(map[key], handler));
            if (stud.Instances == null) {
                return new JSONPkg { Name = null };
            }
            if (stud.Instances[0] == null) {
                return new JSONPkg { Name = null };
            }
            IInventorySTUDInstance instance = stud.Instances[0] as IInventorySTUDInstance;
            if (instance == null) {
                return new JSONPkg { Name = null };
            }

            JSONPkg pkg = new JSONPkg { };
            pkg.Name = Util.GetString(instance.Header.name, map, handler);
            pkg.Description = ListInventory.GetDescription(instance, map, handler);
            pkg.Subline = Util.GetString(instance.Header.availble_in, map, handler);
            pkg.Key = key;
            pkg.Rarity = instance.Header.rarity.ToString();

            if (instance is CreditItem) {
                pkg.Description = $"{(instance as CreditItem).Data.value} Credits";
            } else if (instance is PortraitItem) {
                PortraitItem portrait = instance as PortraitItem;
                string s = "s";
                if (portrait.Data.star == 1) {
                    s = "";
                }
                pkg.Description = $"Tier {portrait.Data.tier} - At least level {(portrait.Data.bracket - 11) * 10 + 1} - {portrait.Data.star} star{s}";
            }

            pkg.Type = instance.Name.ToUpperInvariant();

            return pkg;
        }

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            Dictionary<string, Dictionary<string, Dictionary<string, List<JSONPkg>>>> dict = new Dictionary<string, Dictionary<string, Dictionary<string, List<JSONPkg>>>>();
            foreach (ulong key in track[0x54]) {
                if (!map.ContainsKey(key)) {
                    continue;
                }

                using (Stream input = Util.OpenFile(map[key], handler)) {
                    if (input == null) {
                        continue;
                    }
                    STUD stud = new STUD(input);
                    if (stud.Instances == null || stud.Instances[0] == null) {
                        continue;
                    }

                    GlobalInventoryMaster master = stud.Instances[0] as GlobalInventoryMaster;
                    if (master == null) {
                        continue;
                    }

                    if (!dict.ContainsKey("ACHIEVEMENT")) {
                        dict["ACHIEVEMENT"] = new Dictionary<string, Dictionary<string, List<JSONPkg>>>();
                    }

                    if (!dict.ContainsKey("AVAILABLE")) {
                        dict["AVAILABLE"] = new Dictionary<string, Dictionary<string, List<JSONPkg>>>();
                    }

                    if (!dict.ContainsKey("LOOTBOX_EXCLUSIVE")) {
                        dict["LOOTBOX_EXCLUSIVE"] = new Dictionary<string, Dictionary<string, List<JSONPkg>>>();
                    }
                    
                    for (int i = 0; i < master.Generic.Length; ++i) {
                        if (master.GenericItems[i].Length == 0) {
                            continue;
                        }
                        string s = ItemEvents.GetInstance().GetEvent(master.Generic[i].@event);
                        if (!dict["ACHIEVEMENT"].ContainsKey(s)) {
                            dict["ACHIEVEMENT"][s] = new Dictionary<string, List<JSONPkg>>();
                        }

                        for (int j = 0; j < master.GenericItems[i].Length; ++j) {
                            JSONPkg pkg = GenerateJSONPkg(master.GenericItems[i][j].key, map, handler);
                            if (pkg.Name == null) {
                                continue;
                            }
                            pkg.EventId = master.Generic[i].@event;
                            pkg.Event = s;

                            if (!dict["ACHIEVEMENT"][s].ContainsKey(pkg.Type)) {
                                dict["ACHIEVEMENT"][s][pkg.Type] = new List<JSONPkg>();
                            }
                            dict["ACHIEVEMENT"][s][pkg.Type].Add(pkg);
                        }
                    }
                    
                    for (int i = 0; i < master.Categories.Length; ++i) {
                        if (master.CategoryItems[i].Length == 0) {
                            continue;
                        }
                        
                        string s = ItemEvents.GetInstance().GetEvent(master.Categories[i].@event);
                        if (!dict["AVAILABLE"].ContainsKey(s)) {
                            dict["AVAILABLE"][s] = new Dictionary<string, List<JSONPkg>>();
                        }
                        
                        for (int j = 0; j < master.CategoryItems[i].Length; ++j) {
                            JSONPkg pkg = GenerateJSONPkg(master.CategoryItems[i][j].key, map, handler);
                            if (pkg.Name == null) {
                                continue;
                            }
                            pkg.EventId = master.Categories[i].@event;
                            pkg.Event = s;

                            if (!dict["AVAILABLE"][s].ContainsKey(pkg.Type)) {
                                dict["AVAILABLE"][s][pkg.Type] = new List<JSONPkg>();
                            }
                            dict["AVAILABLE"][s][pkg.Type].Add(pkg);
                        }
                    }

                    for (int i = 0; i < master.ExclusiveOffsets.Length; ++i) {
                        if (master.LootboxExclusive[i].Length == 0) {
                            continue;
                        }

                        string s = ItemEvents.GetInstance().GetEvent((ulong)i);
                        if (!dict["LOOTBOX_EXCLUSIVE"].ContainsKey(s)) {
                            dict["LOOTBOX_EXCLUSIVE"][s] = new Dictionary<string, List<JSONPkg>>();
                        }
                        
                        for (int j = 0; j < master.LootboxExclusive[i].Length; ++j) {
                            JSONPkg pkg = GenerateJSONPkg(master.LootboxExclusive[i][j].item, map, handler);
                            if (pkg.Name == null) {
                                continue;
                            }
                            pkg.EventId = (ulong)i;
                            pkg.Event = s;

                            if (!dict["LOOTBOX_EXCLUSIVE"][s].ContainsKey(pkg.Type)) {
                                dict["LOOTBOX_EXCLUSIVE"][s][pkg.Type] = new List<JSONPkg>();
                            }
                            dict["LOOTBOX_EXCLUSIVE"][s][pkg.Type].Add(pkg);
                        }
                    }
                }
            }

            if (Path.GetDirectoryName(flags.Positionals[2]).Trim().Length > 0 && !Directory.Exists(Path.GetDirectoryName(flags.Positionals[2]))) {
                Directory.CreateDirectory(Path.GetDirectoryName(flags.Positionals[2]));
            }
            using (Stream file = File.OpenWrite(flags.Positionals[2])) {
                using (TextWriter writer = new StreamWriter(file)) {
                    writer.Write(JsonConvert.SerializeObject(dict, Formatting.Indented));
                }
            }
        }
    }
}
