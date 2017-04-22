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

        public ushort[] Track => new ushort[2] { 0xA5, 0x75 };

        private static void ExtractImage(ulong imageKey, string dpath, Dictionary<ulong, Record> map, CASCHandler handler, string name = null) {
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

            Console.Out.WriteLine("Wrote image {0}", path);
        }

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
            string output = args[0];
            List<char> blank = new List<char>();
            HashSet<ulong> skip = new HashSet<ulong>();
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

                if (master.Header.itemMaster.key == 0) { // AI
                    continue;
                }

                STUD inventoryStud = new STUD(Util.OpenFile(map[master.Header.itemMaster.key], handler));
                InventoryMaster inventory = (InventoryMaster)inventoryStud.Instances[0];
                if (inventory == null) {
                    continue;
                }

                foreach (OWRecord record in inventory.Achievables) {
                    skip.Add(record);
                }

                for (int i = 0; i < inventory.DefaultGroups.Length; ++i) {
                    if (inventory.Defaults[i].Length == 0) {
                        continue;
                    }
                    OWRecord[] records = inventory.Defaults[i];
                    foreach (OWRecord record in records) {
                        skip.Add(record);
                    }
                }

                for (int i = 0; i < inventory.ItemGroups.Length; ++i) {
                    if (inventory.Items[i].Length == 0) {
                        continue;
                    }
                    OWRecord[] records = inventory.Items[i];
                    foreach (OWRecord record in records) {
                        skip.Add(record);
                    }
                }
            }

            foreach (ulong key in track[0xA5]) {
                if (skip.Contains(key)) {
                    continue;
                }
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

                    IInventorySTUDInstance instance = stud.Instances[0] as IInventorySTUDInstance;
                    if (instance == null) {
                        continue;
                    }
                    string name = Util.GetString(instance.Header.name.key, map, handler);
                    if (name == null) {
                        name = $"{GUID.Index(key):X8}";
                    }

                    string path = string.Format("{0}{1}General{1}{2}{1}", output, Path.DirectorySeparatorChar, stud.Instances[0].Name);

                    switch (stud.Instances[0].Name) {
                        case "Icon":
                            ExtractLogic.Icon.Extract(stud, output, "General", name, "", track, map, handler, blank);
                            break;
                        case "Spray":
                            ExtractLogic.Spray.Extract(stud, output, "General", name, "", track, map, handler, blank);
                            break;
                        case "Portrait":
                            PortraitItem portrait = instance as PortraitItem;
                            ExtractLogic.Portrait.Extract(stud, output, "General", $"Tier {portrait.Data.tier}", "", track, map, handler, blank);
                            break;
                        default: continue;
                    }
                }
            }
        }
    }
}
