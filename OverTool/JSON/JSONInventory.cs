using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using Newtonsoft.Json;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;
using static OverTool.JSON.JSONGeneral;

namespace OverTool.JSON {
    public class JSONInventory : IOvertool {
        public string Title => "JSON output hero cosmetics";
        public char Opt => '\0';
        public string FullOpt => "json-list";
        public string Help => "output.json";
        public uint MinimumArgs => 1;
        public ushort[] Track => new ushort[1] { 0x75 };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, List<JSONPkg>>>>> dict_upper = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, List<JSONPkg>>>>>();
            foreach (ulong key in track[0x75]) {
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

                    HeroMaster hero = stud.Instances[0] as HeroMaster;
                    if (hero == null) {
                        continue;
                    }
                    Dictionary<string, Dictionary<string, Dictionary<string, List<JSONPkg>>>> dict = new Dictionary<string, Dictionary<string, Dictionary<string, List<JSONPkg>>>>();


                    string heroName = Util.GetString(hero.Header.name.key, map, handler);
                    if (heroName == null) {
                        continue;
                    }
                    if (hero.Header.itemMaster.key == 0) { // AI
                        continue;
                    }

                    if (!map.ContainsKey(hero.Header.itemMaster.key)) {
                        continue;
                    }
                STUD inventoryStud = new STUD(Util.OpenFile(map[hero.Header.itemMaster.key], handler));
                InventoryMaster master = (InventoryMaster)inventoryStud.Instances[0];
                    if (master == null) {
                        continue;
                    }

                    if (!dict.ContainsKey("ACHIEVEMENT")) {
                        dict["ACHIEVEMENT"] = new Dictionary<string, Dictionary<string, List<JSONPkg>>>();
                    }

                    if (!dict.ContainsKey("STANDARD")) {
                        dict["STANDARD"] = new Dictionary<string, Dictionary<string, List<JSONPkg>>>();
                    }

                    if (!dict.ContainsKey("AVAILABLE")) {
                        dict["AVAILABLE"] = new Dictionary<string, Dictionary<string, List<JSONPkg>>>();
                    }

                    string s = ItemEvents.GetInstance().GetEvent(0);
                    if (!dict["ACHIEVEMENT"].ContainsKey(s)) {
                        dict["ACHIEVEMENT"][s] = new Dictionary<string, List<JSONPkg>>();
                    }

                    for (int i = 0; i < master.Achievables.Length; ++i) {
                        JSONPkg pkg = JSONGeneral.GenerateJSONPkg(master.Achievables[i], map, handler);
                        if (pkg.Name == null) {
                            continue;
                        }
                        pkg.EventId = 0;
                        pkg.Event = s;

                        if (!dict["ACHIEVEMENT"][s].ContainsKey(pkg.Type)) {
                            dict["ACHIEVEMENT"][s][pkg.Type] = new List<JSONPkg>();
                        }
                        dict["ACHIEVEMENT"][s][pkg.Type].Add(pkg);
                    }

                    for (int i = 0; i < master.DefaultGroups.Length; ++i) {
                        if (master.Defaults[i].Length == 0) {
                            continue;
                        }

                        s = ItemEvents.GetInstance().GetEvent(master.DefaultGroups[i].@event);
                        if (!dict["STANDARD"].ContainsKey(s)) {
                            dict["STANDARD"][s] = new Dictionary<string, List<JSONPkg>>();
                        }

                        for (int j = 0; j < master.Defaults[i].Length; ++j) {
                            JSONPkg pkg = GenerateJSONPkg(master.Defaults[i][j].key, map, handler);
                            if (pkg.Name == null) {
                                continue;
                            }
                            pkg.EventId = master.DefaultGroups[i].@event;
                            pkg.Event = s;

                            if (!dict["STANDARD"][s].ContainsKey(pkg.Type)) {
                                dict["STANDARD"][s][pkg.Type] = new List<JSONPkg>();
                            }
                            dict["STANDARD"][s][pkg.Type].Add(pkg);
                        }
                    }
                    

                    for (int i = 0; i < master.ItemGroups.Length; ++i) {
                        if (master.Items[i].Length == 0) {
                            continue;
                        }

                        s = ItemEvents.GetInstance().GetEvent(master.ItemGroups[i].@event);
                        if (!dict["AVAILABLE"].ContainsKey(s)) {
                            dict["AVAILABLE"][s] = new Dictionary<string, List<JSONPkg>>();
                        }

                        for (int j = 0; j < master.Items[i].Length; ++j) {
                            JSONPkg pkg = GenerateJSONPkg(master.Items[i][j].key, map, handler);
                            if (pkg.Name == null) {
                                continue;
                            }
                            pkg.EventId = master.ItemGroups[i].@event;
                            pkg.Event = s;

                            if (!dict["AVAILABLE"][s].ContainsKey(pkg.Type)) {
                                dict["AVAILABLE"][s][pkg.Type] = new List<JSONPkg>();
                            }
                            dict["AVAILABLE"][s][pkg.Type].Add(pkg);
                        }
                    }

                    dict_upper[heroName] = dict;
                }
            }

            if (Path.GetDirectoryName(flags.Positionals[2]).Trim().Length > 0 && !Directory.Exists(Path.GetDirectoryName(flags.Positionals[2]))) {
                Directory.CreateDirectory(Path.GetDirectoryName(flags.Positionals[2]));
            }
            using (Stream file = File.OpenWrite(flags.Positionals[2])) {
                using (TextWriter writer = new StreamWriter(file)) {
                    writer.Write(JsonConvert.SerializeObject(dict_upper, Formatting.Indented));
                }
            }
        }
    }
}
