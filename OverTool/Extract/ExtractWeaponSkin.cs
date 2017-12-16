using System;
using System.Collections.Generic;
using System.Linq;
using CASCLib;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
    class ExtractWeaponSkin : IOvertool {
        public string Help => "output [query]";
        public uint MinimumArgs => 1;
        public char Opt => 'w';
        public string FullOpt => "weaponskin";
        public string Title => "Extract Hero Weapon Skins";
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

            HashSet<string> heroes = new HashSet<string>();
            HashSet<string> heroBlank = new HashSet<string>();
            Dictionary<string, HashSet<string>> heroSkin = new Dictionary<string, HashSet<string>>();
            bool heroAllWildcard = false;
            if (flags.Positionals.Length > 3 && flags.Positionals[1] != "*") {
                foreach (string name in flags.Positionals[3].ToLowerInvariant().Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries)) {
                    string[] data = name.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    string realname = data[0];
                    heroes.Add(realname);
                    data = data.Skip(1).ToArray();
                    if (data.Length == 0) {
                        heroBlank.Add(realname);
                        continue;
                    }
                    heroSkin[realname] = new HashSet<string>();
                    foreach (string skin in data) {
                        heroSkin[realname].Add(skin);
                    }
                }
            } else {
                heroAllWildcard = true;
            }
            
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
                    heroes.Add(heroName.ToLowerInvariant());
                    heroBlank.Add(heroName.ToLowerInvariant());
                }
                if (!heroes.Contains(heroName.ToLowerInvariant())) {
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

                Dictionary<string, WeaponSkinItem> weaponskins = new Dictionary<string, WeaponSkinItem>();
                Console.Out.WriteLine("Finding weapon skins for {0}", heroName);
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
                    WeaponSkinItem weapon = instance as WeaponSkinItem;
                    if (weapon == null) {
                        continue;
                    }
                    if (weapon.Data.index == 0) {
                        continue;
                    }
                    string name = Util.GetString(instance.Header.name.key, map, handler);
                    if (name == null) {
                        name = $"Untitled-{GUID.LongKey(instance.Header.name.key):X12}";
                        continue;
                    }
                    weaponskins[name] = weapon;
                    Console.Out.WriteLine("Found data for weapon skin {0}", name);
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
                    SkinItem skin = instance as SkinItem;
                    if (skin == null) {
                        continue;
                    }

                    string name = Util.GetString(instance.Header.name.key, map, handler);
                    if (name == null) {
                        name = $"Untitled-{GUID.LongKey(instance.Header.name.key):X12}";
                        continue;
                    }
                    if (heroBlank.Contains(heroName.ToLowerInvariant()) || heroSkin[heroName.ToLowerInvariant()].Contains(name.ToLowerInvariant())) {
                        Console.Out.WriteLine("Saving textures for skin {0}", name);
                        foreach (KeyValuePair<string, WeaponSkinItem> pair in weaponskins) {
                            string output_real = string.Format("{0}{1}{4}{1}Weapon Skin{1}{2}{1}{3}{1}", output, System.IO.Path.DirectorySeparatorChar, Util.SanitizePath(pair.Key), Util.SanitizePath(name), Util.SanitizePath(heroName));
                            Dictionary<ulong, ulong> replace = new Dictionary<ulong, ulong>();
                            ExtractLogic.Skin.FindReplacements(skin.Data.skin.key, (int)pair.Value.Data.index, replace, new HashSet<ulong>(), map, handler, master, skin, true);
                            Dictionary<ulong, ulong> replace_blank = new Dictionary<ulong, ulong>();
                            ExtractLogic.Skin.FindReplacements(skin.Data.skin.key, -1, replace_blank, new HashSet<ulong>(), map, handler, master, skin, false);
                            if (replace.Values.Count == 0) {
                                continue;
                            }
                            Dictionary<ulong, List<ImageLayer>> layers = new Dictionary<ulong, List<ImageLayer>>();
                            Dictionary<ulong, List<ImageLayer>> layers_orig = new Dictionary<ulong, List<ImageLayer>>();
                            foreach (KeyValuePair<ulong, ulong> texture_pair in replace) {
                                ushort type = GUID.Type(texture_pair.Key);
                                switch (type) {
                                    case 0x1A:
                                        ExtractLogic.Skin.FindTextures(texture_pair.Value, layers, replace, new HashSet<ulong>(), map, handler);
                                        if (replace_blank.ContainsKey(texture_pair.Key)) {
                                            ExtractLogic.Skin.FindTextures(replace_blank[texture_pair.Key], layers_orig, replace_blank, new HashSet<ulong>(), map, handler);
                                        } else {
                                            ExtractLogic.Skin.FindTextures(texture_pair.Key, layers_orig, replace_blank, new HashSet<ulong>(), map, handler);
                                        }
                                        break;
                                }
                            }

                            Dictionary<ulong, HashSet<ulong>> origTextures = new Dictionary<ulong, HashSet<ulong>>();
                            foreach (KeyValuePair<ulong, List<ImageLayer>> kv in layers_orig) {
                                ulong materialId = kv.Key;
                                List<ImageLayer> sublayers = kv.Value;
                                HashSet<ulong> materialParsed = new HashSet<ulong>();
                                if (!origTextures.ContainsKey(materialId)) {
                                    origTextures[materialId] = new HashSet<ulong>();
                                }
                                foreach (ImageLayer layer in sublayers) {
                                    origTextures[materialId].Add(layer.Key);
                                }
                            }

                            foreach (KeyValuePair<ulong, List<ImageLayer>> kv in layers) {
                                ulong materialId = kv.Key;
                                List<ImageLayer> sublayers = kv.Value;
                                HashSet<ulong> materialParsed = new HashSet<ulong>();
                                foreach (ImageLayer layer in sublayers) {
                                    if (!materialParsed.Add(layer.Key)) {
                                        continue;
                                    }
                                    if (origTextures.ContainsKey(materialId) && origTextures[materialId].Contains(layer.Key)) {
                                        continue;
                                    }
                                    ExtractLogic.Skin.SaveTexture(layer.Key, materialId, map, handler, output_real, quiet, "");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
