using System;
using System.Collections.Generic;
using System.Linq;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
    class Extract : IOvertool {
        public string Help => "output [types [query]]]";
        public uint MinimumArgs => 1;
        public char Opt => 'x';
        public string FullOpt => "extract";
        public string Title => "Extract Hero Cosmetics";
        public ushort[] Track => new ushort[1] { 0x75 };
        public bool Display => true;

        public static InventoryMaster OpenInventoryMaster(HeroMaster master, Dictionary<ulong, Record> map, CASCHandler handler) {
            if (!map.ContainsKey(master.Header.itemMaster.key)) {
                return null;
            }
            STUD inventoryStud = new STUD(Util.OpenFile(map[master.Header.itemMaster.key], handler));
            InventoryMaster inventory = (InventoryMaster)inventoryStud.Instances[0];
            if (inventory == null) {
                return null;
            }
            return inventory;
        }

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            string output = flags.Positionals[2];

            string[] validCommands = new string[] { "skin", "spray", "icon", "victory pose", "emote", "heroic intro" };

            bool typeWildcard = true;
            List<string> types = new List<string>();
            if (flags.Positionals.Length > 3) {
                types.AddRange(flags.Positionals[3].ToLowerInvariant().Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries));
                typeWildcard = types.Contains("*");
                types = types.FindAll((string it) => validCommands.Contains(it)).ToList();
            }

            if (!typeWildcard && types.Count == 0) {
                string cmdlist = "";
                for (int i = 0; i < validCommands.Length; i++) {
                    if (i != 0) {
                        cmdlist += ", ";
                        if (i == validCommands.Length - 1) {
                            cmdlist += "or ";
                        }
                    }
                    cmdlist += validCommands[i].ToString().ToLower();
                }
                Console.Out.WriteLine("The output type was not a valid option. ({0})", cmdlist);
                return;
            }

            Dictionary<string, List<string>> heroTypes = new Dictionary<string, List<string>>();
            Dictionary<string, bool> heroWildcard = new Dictionary<string, bool>();
            Dictionary<string, Dictionary<string, List<ulong>>> heroIgnore = new Dictionary<string, Dictionary<string, List<ulong>>>();
            bool heroAllWildcard = false;
            if (flags.Positionals.Length > 4 && flags.Positionals[4] != "*") {
                foreach (string pair in flags.Positionals[4].ToLowerInvariant().Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries)) {
                    List<string> data = new List<string>(pair.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries));
                    string name = data[0];
                    data.RemoveAt(0);

                    if (!heroTypes.ContainsKey(name)) {
                        heroTypes[name] = new List<string>();
                        heroIgnore[name] = new Dictionary<string, List<ulong>>();
                        heroWildcard[name] = false;
                    }

                    if (data.Count > 0) {
                        foreach (string d in data) {
                            List<string> subdata = new List<string>(d.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
                            string subn = subdata[0];
                            subdata.RemoveAt(0);
                            heroTypes[name].Add(subn);
                            heroIgnore[name][subn] = new List<ulong>();
                            if (subdata.Count > 0) {
                                foreach (string sd in subdata) {
                                    try {
                                        heroIgnore[name][subn].Add(ulong.Parse(sd, System.Globalization.NumberStyles.HexNumber));
                                    } catch { }
                                }
                            }
                        }
                    }

                    if (data.Count == 0 || data.Contains("*")) {
                        heroWildcard[name] = true;
                    }
                }
            } else {
                heroAllWildcard = true;
            }

            int replacementIndex = flags.WeaponSkinIndex;

            List<ulong> masters = track[0x75];
            foreach (ulong masterKey in masters) {
                if (!map.ContainsKey(masterKey)) {
                    continue;
                }
                STUD masterStud = new STUD(Util.OpenFile(map[masterKey], handler));
                if (masterStud.Instances == null) {
                    continue;
                }
                HeroMaster master = (HeroMaster)masterStud.Instances[0];
                if (master == null) {
                    continue;
                }
                if (master.Header.itemMaster.key == 0) {
                    continue;
                }
                string heroName = Util.GetString(master.Header.name.key, map, handler);
                if (heroName == null) {
                    continue;
                }
                if (heroAllWildcard) {
                    if (!heroTypes.ContainsKey(heroName.ToLowerInvariant())) {
                        heroTypes.Add(heroName.ToLowerInvariant(), new List<string>());
                    }
                    if (!heroWildcard.ContainsKey(heroName.ToLowerInvariant())) {
                        heroWildcard.Add(heroName.ToLowerInvariant(), true);
                    }
                }
                if (!heroTypes.ContainsKey(heroName.ToLowerInvariant())) {
                    continue;
                }
                InventoryMaster inventory = OpenInventoryMaster(master, map, handler);
                if (inventory == null) {
                    continue;
                }

                Dictionary<OWRecord, string> items = new Dictionary<OWRecord, string>();
                foreach (OWRecord record in inventory.Achievables) {
                    items[record] = "ACHIEVEMENT";
                }

                for (int i = 0; i < inventory.DefaultGroups.Length; ++i) {
                    string name = $"STANDARD_{ItemEvents.GetInstance().GetEvent(inventory.DefaultGroups[i].@event)}";
                    for (int j = 0; j < inventory.Defaults[i].Length; ++j) {
                        items[inventory.Defaults[i][j]] = name;
                    }
                }

                for (int i = 0; i < inventory.ItemGroups.Length; ++i) {
                    string name = ItemEvents.GetInstance().GetEvent(inventory.ItemGroups[i].@event);
                    for (int j = 0; j < inventory.Items[i].Length; ++j) {
                        items[inventory.Items[i][j]] = name;
                    }
                }

                foreach (KeyValuePair<OWRecord, string> recordname in items) {
                    OWRecord record = recordname.Key;
                    string itemGroup = recordname.Value;
                    if (!map.ContainsKey(record.key)) {
                        continue;
                    }

                    STUD stud = new STUD(Util.OpenFile(map[record.key], handler));
                    if (stud.Instances == null) {
                        continue;
                    }
                    IInventorySTUDInstance instance = (IInventorySTUDInstance)stud.Instances[0];
                    if (!typeWildcard && !types.Contains(instance.Name.ToLowerInvariant())) {
                        continue;
                    }

                    string name = Util.GetString(instance.Header.name.key, map, handler);
                    if (name == null) {
                        name = $"Untitled-{GUID.LongKey(instance.Header.name.key):X12}";
                        continue;
                    }
                    if (!heroWildcard[heroName.ToLowerInvariant()] && !heroTypes[heroName.ToLowerInvariant()].Contains(name.ToLowerInvariant())) {
                        continue;
                    }

                    switch (instance.Name) {
                        case "Spray":
                            Console.Out.WriteLine("Extracting spray {0} for {1}...", name, heroName);
                            ExtractLogic.Spray.Extract(stud, output, heroName, name, itemGroup, track, map, handler, quiet, flags);
                            break;
                        case "Skin":
                            List<ulong> ignoreList = new List<ulong>();
                            try {
                                ignoreList = heroIgnore[heroName.ToLowerInvariant()][name.ToLowerInvariant()];
                            } catch { }
                            Console.Out.WriteLine("Extracting {0} models and textures for {1}", name, heroName);
                            ExtractLogic.Skin.Extract(master, stud, output, heroName, name, itemGroup, ignoreList, track, map, handler, quiet, flags, masterKey, replacementIndex);
                            break;
                        case "Icon":
                            Console.Out.WriteLine("Extracting icon {0} for {1}...", name, heroName);
                            ExtractLogic.Icon.Extract(stud, output, heroName, name, itemGroup, track, map, handler, quiet, flags);
                            break;
                        case "Emote":
                        case "Victory Pose":
                        case "Heroic Intro":
                            Console.Out.WriteLine("Extracting {2} {0} for {1}...", name, heroName, instance.Name);
                            ExtractLogic.ItemAnimation.Extract(record.key, stud, output, heroName, name, itemGroup, track, map, handler, quiet, flags);
                            break;
                        case "Voice Line":
                            //Console.Out.WriteLine("Extracting voice line {0} for {1}...", name, heroName);
                            //ExtractLogic.VoiceLine.Extract(master, stud, output, heroName, name, track, map, handler);
                            break;
                    }
                }
            }
        }
    }
}
