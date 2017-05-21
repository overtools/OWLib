using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
    class ListWeapon : IOvertool {
        public string Help => "No additional arguments";
        public uint MinimumArgs => 0;
        public char Opt => 'W';
        public string Title => "List Weapon Override Index";
        public ushort[] Track => new ushort[1] { 0x75 };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, string[] args) {
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
                bool ex = System.Diagnostics.Debugger.IsAttached;
                List<string> largs = new List<string>(args);
                if (largs.Count > 0 && largs.Contains("ex")) {
                    ex = true;
                }
                bool hasName = false;
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

                Dictionary<int, string> indexMap = new Dictionary<int, string>();
                List<OWRecord> items = new List<OWRecord>();

                items.AddRange(inventory.Achievables.ToList());

                for (int i = 0; i < inventory.DefaultGroups.Length; ++i) {
                    string name = $"STANDARD_{ItemEvents.GetInstance().GetEvent(inventory.DefaultGroups[i].@event)}";
                    items.AddRange(inventory.Defaults[i].ToList());
                }

                for (int i = 0; i < inventory.ItemGroups.Length; ++i) {
                    string name = ItemEvents.GetInstance().GetEvent(inventory.ItemGroups[i].@event);
                    items.AddRange(inventory.Items[i].ToList());
                }

                foreach (OWRecord record in items) {
                    STUD item = new STUD(Util.OpenFile(map[record], handler));
                    if (item.Instances == null) {
                        continue;
                    }
                    WeaponSkinItem skin = item.Instances[0] as WeaponSkinItem;
                    if (skin == null) {
                        continue;
                    }
                    if (!hasName) {
                        if (ex) {
                            Console.Out.WriteLine("Weapons for {0} ({2:X16}) in package {1:X16}", heroName, map[masterKey].package.packageKey, masterKey);
                        } else {
                            Console.Out.WriteLine("Weapons for {0}", heroName);
                        }
                        hasName = true;
                    }

                    Console.Out.WriteLine("\tIndex {0} - {1}", skin.Data.index, Util.GetString(skin.Header.name, map, handler));
                }

                if (hasName) {
                    Console.Out.WriteLine("");
                }
            }
        }
    }
}
