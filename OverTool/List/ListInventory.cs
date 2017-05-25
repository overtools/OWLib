using System;
using System.IO;
using System.Collections.Generic;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
    class ListInventory : IOvertool {
        public string Help => "No additional arguments";
        public uint MinimumArgs => 0;
        public char Opt => 't';
        public string Title => "List Hero Cosmetics";
        public ushort[] Track => new ushort[1] { 0x75 };
        public bool Display => true;

        public static void GetInventoryName(ulong key, bool ex, Dictionary<ulong, Record> map, CASCHandler handler, string heroName) {
            if (!map.ContainsKey(key)) {
                return;
            }

            STUD stud = new STUD(Util.OpenFile(map[key], handler));
            if (stud.Instances == null) {
                return;
            }
            if (stud.Instances[0] == null) {
                return;
            }
            IInventorySTUDInstance instance = (IInventorySTUDInstance)stud.Instances[0];
            if (instance == null) {
                return;
            }

            string name = Util.GetString(instance.Header.name.key, map, handler);
            if (name == null && instance.Name != stud.Manager.GetName(typeof(CreditItem))) {
                Console.Out.WriteLine("\t\t(Untitled-{0:X12}) ({1} {2})", GUID.LongKey(key), instance.Header.rarity, stud.Instances[0].Name);
                return;
            }
#if OUTPUT_STUDINVENTORY
            Stream studStream = Util.OpenFile(map[key], handler);
            string filename = string.Format("{0}_{1}", instance.Name, name);
            foreach (var c in Path.GetInvalidFileNameChars()) {
                filename = filename.Replace(c, '_');
            }
            string outFilename = string.Format("./STUDs/Inventory/{0}/{1}.stud", heroName, filename);
            string putPathname = outFilename.Substring(0, outFilename.LastIndexOf('/'));
            Directory.CreateDirectory(putPathname);
            Stream OutWriter = File.Create(outFilename);
            studStream.CopyTo(OutWriter);
            OutWriter.Close();
            studStream.Close();
#endif

            string addt = string.Empty;

            if (instance.Name == stud.Manager.GetName(typeof(CreditItem))) {
                CreditItem credits = (CreditItem)instance;
                name = $"{credits.Data.value} Credits";
            }
            if (instance.Name == stud.Manager.GetName(typeof(WeaponSkinItem))) {
                WeaponSkinItem weapon = (WeaponSkinItem)instance;
                addt = $" Index {weapon.Data.index}";
            }

            if (ex) {
                Console.Out.WriteLine("\t\t{0} ({1} {2} in package {3:X16}){4}", name, instance.Header.rarity, stud.Instances[0].Name, map[key].package.packageKey, addt);
            } else {
                Console.Out.WriteLine("\t\t{0} ({1} {2}){3}", name, instance.Header.rarity, stud.Instances[0].Name, addt);
            }

            if (instance.Header.description != 0) {
                using (Stream descStream = Util.OpenFile(map[instance.Header.description], handler)) {
                    STUD description = new STUD(descStream);
                    if (description.Instances == null) {
                        return;
                    }
                    InventoryDescription desc = description.Instances[0] as InventoryDescription;
                    if (desc == null) {
                        return;
                    }
                    string descriptionStr = Util.GetString(desc.Header.str, map, handler);
                    if (!string.IsNullOrEmpty(descriptionStr)) {
                        Console.Out.WriteLine("\t\t\t{0}", descriptionStr);
                    }
                }
            }
        }

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
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
                string heroName = Util.GetString(master.Header.name.key, map, handler);
                if (heroName == null) {
                    continue;
                }
                string goodhero = heroName;
                foreach (var c in Path.GetInvalidFileNameChars()) {
                    goodhero = goodhero.Replace(c, '_');
                }

                if (master.Header.itemMaster.key == 0) { // AI
                    continue;
                }
                bool ex = System.Diagnostics.Debugger.IsAttached || flags.Expert;
                if (ex) {
                    Console.Out.WriteLine("Cosmetics for {0} ({2:X16}) in package {1:X16}", heroName, map[masterKey].package.packageKey, masterKey);
                } else {
                    Console.Out.WriteLine("Cosmetics for {0}", heroName);
                }
                if (!map.ContainsKey(master.Header.itemMaster.key)) {
                    Console.Out.WriteLine("Error loading inventory master file...");
                    continue;
                }
                STUD inventoryStud = new STUD(Util.OpenFile(map[master.Header.itemMaster.key], handler));
#if OUTPUT_STUDINVENTORYMASTER
                Stream studStream = Util.OpenFile(map[master.Header.itemMaster.key], handler);
                string outFilename = string.Format("./STUDs/ListInventoryMaster/{0}.stud", goodhero);
                string putPathname = outFilename.Substring(0, outFilename.LastIndexOf('/'));
                Directory.CreateDirectory(putPathname);
                Stream OutWriter = File.Create(outFilename);
                studStream.CopyTo(OutWriter);
                OutWriter.Close();
                studStream.Close();
#endif
                InventoryMaster inventory = (InventoryMaster)inventoryStud.Instances[0];
                if (inventory == null) {
                    Console.Out.WriteLine("Error loading inventory master file...");
                    continue;
                }

                Console.Out.WriteLine("\tACHIEVEMENT ({0} items)", inventory.Achievables.Length);
                foreach (OWRecord record in inventory.Achievables) {
                    GetInventoryName(record.key, ex, map, handler, goodhero);
                }

                for (int i = 0; i < inventory.DefaultGroups.Length; ++i) {
                    if (inventory.Defaults[i].Length == 0) {
                        continue;
                    }
                    OWRecord[] records = inventory.Defaults[i];
                    Console.Out.WriteLine("\tSTANDARD_{0} ({1} items)", ItemEvents.GetInstance().GetEvent(inventory.DefaultGroups[i].@event), records.Length);
                    foreach (OWRecord record in records) {
                        GetInventoryName(record.key, ex, map, handler, goodhero);
                    }
                }

                for (int i = 0; i < inventory.ItemGroups.Length; ++i) {
                    if (inventory.Items[i].Length == 0) {
                        continue;
                    }
                    OWRecord[] records = inventory.Items[i];
                    Console.Out.WriteLine("\t{0} ({1} items)", ItemEvents.GetInstance().GetEvent(inventory.ItemGroups[i].@event), records.Length);
                    foreach (OWRecord record in records) {
                        GetInventoryName(record.key, ex, map, handler, goodhero);
                    }
                }
                Console.Out.WriteLine("");
            }
        }
    }
}
