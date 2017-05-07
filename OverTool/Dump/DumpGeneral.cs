using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
    public class DumpGeneral : IOvertool {
        public string Title => "Extract General Cosmetics";
        public char Opt => 'G';
        public string Help => "output";
        public uint MinimumArgs => 1;
        public ushort[] Track => new ushort[1] { 0x54 };
        public bool Display => true;

        private static void ExtractImage(ulong imageKey, string dpath, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, string name = null) {
            ulong imageDataKey = (imageKey & 0xFFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL;
            if (string.IsNullOrWhiteSpace(name)) {
                name = $"{GUID.LongKey(imageKey):X12}";
            }
            string path = $"{dpath}{name}.dds";
            if (!Directory.Exists(Path.GetDirectoryName(path))) {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            using (Stream outp = File.Open(path, FileMode.Create, FileAccess.Write)) {
                if (map.ContainsKey(imageDataKey)) {
                    Texture tex = new Texture(Util.OpenFile(map[imageKey], handler), Util.OpenFile(map[imageDataKey], handler));
                    tex.Save(outp);
                } else {
                    TextureLinear tex = new TextureLinear(Util.OpenFile(map[imageKey], handler));
                    tex.Save(outp);
                }
            }

            if (!quiet) {
                Console.Out.WriteLine("Wrote image {0}", path);
            }
        }

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, string[] args) {
            string output = args[0];
            List<char> blank = new List<char>();

            foreach (ulong key in track[0x54]) {
                if (!map.ContainsKey(key)) {
                    continue;
                }
                Dictionary<OWRecord, string> items = new Dictionary<OWRecord, string>();
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

                    foreach (OWRecord record in master.StandardItems) {
                        items[record] = "ACHIEVEMENT";
                    }

                    for (int i = 0; i < master.Generic.Length; ++i) {
                        string name = $"STANDARD_{ItemEvents.GetInstance().GetEvent(master.Generic[i].@event)}";
                        for (int j = 0; j < master.GenericItems[i].Length; ++j) {
                            items[master.GenericItems[i][j]] = name;
                        }
                    }

                    for (int i = 0; i < master.Categories.Length; ++i) {
                        string name = ItemEvents.GetInstance().GetEvent(master.Categories[i].@event);
                        for (int j = 0; j < master.CategoryItems[i].Length; ++j) {
                            items[master.CategoryItems[i][j]] = name;
                        }
                    }

                    for (int i = 0; i < master.ExclusiveOffsets.Length; ++i) {
                        string name = $"LOOTBOX_EXCLUSIVE_{i:X}";
                        for (int j = 0; j < master.LootboxExclusive[i].Length; ++j) {
                            items[master.LootboxExclusive[i][j].item] = name;
                        }
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
                    string name = Util.GetString(instance.Header.name.key, map, handler);
                    if (name == null) {
                        name = $"{GUID.LongKey(key):X12}";
                    }

                    switch (instance.Name) {
                        case "Spray":
                            Console.Out.WriteLine("Extracting spray {0}...", name);
                            ExtractLogic.Spray.Extract(stud, output, "Generic", name, itemGroup, track, map, handler, quiet, blank);
                            break;
                        case "Icon":
                            Console.Out.WriteLine("Extracting icon {0}...", name);
                            ExtractLogic.Icon.Extract(stud, output, "Generic", name, itemGroup, track, map, handler, quiet, blank);
                            break;
                        case "Portrait":
                            PortraitItem portrait = instance as PortraitItem;
                            ExtractLogic.Portrait.Extract(stud, output, "Generic", $"Tier {portrait.Data.tier}", itemGroup, track, map, handler, quiet, blank);
                            break;
                        default:
                            continue;
                    }
                }
            }
        }
    }
}
