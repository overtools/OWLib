using System;
using System.Collections.Generic;
using System.IO;
using CASCLib;
using OverTool.ExtractLogic;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
    class DumpVoiceFull : IOvertool {
        public string Help => "output [hero query]";
        public uint MinimumArgs => 1;
        public char Opt => 'V';
        public string FullOpt => "voice";
        public string Title => "Extract sounds from all skins";
        public ushort[] Track => new ushort[1] { 0x75 };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            string output = flags.Positionals[2];

            List<string> heroes = new List<string>();
            if (flags.Positionals.Length > 3) {
                heroes.AddRange(flags.Positionals[3].ToLowerInvariant().Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries));
            }
            bool heroAllWildcard = heroes.Count == 0 || heroes.Contains("*");

            List<ulong> masters = track[0x75];
            foreach (ulong masterKey in masters) {
                if (!map.ContainsKey(masterKey)) {
                    continue;
                }
                STUD masterStud = new STUD(Util.OpenFile(map[masterKey], handler));
                if (masterStud.Instances == null || masterStud.Instances[0] == null) {
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
                if (!heroes.Contains(heroName.ToLowerInvariant())) {
                    if (!heroAllWildcard) {
                        continue;
                    }
                }
                HashSet<ulong> items = new HashSet<ulong>();
                InventoryMaster inventory = Extract.OpenInventoryMaster(master, map, handler);
                if (inventory == null) {
                    continue;
                }

                foreach (OWRecord record in inventory.Achievables) {
                    items.Add(record);
                }
                for (int i = 0; i < inventory.DefaultGroups.Length; ++i) {
                    for (int j = 0; j < inventory.Defaults[i].Length; ++j) {
                        items.Add(inventory.Defaults[i][j]);
                    }
                }
                for (int i = 0; i < inventory.ItemGroups.Length; ++i) {
                    for (int j = 0; j < inventory.Items[i].Length; ++j) {
                        items.Add(inventory.Items[i][j]);
                    }
                }
                
                HashSet<ulong> done = new HashSet<ulong>();

                string path = string.Format("{0}{1}{2}{1}{3}{1}", output, Path.DirectorySeparatorChar, Util.Strip(Util.SanitizePath(heroName)), "Sound Dump Full");
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }

                bool haveBase = false;

                foreach (ulong itemKey in items) {
                    if (!map.ContainsKey(itemKey)) {
                        continue;
                    }
                    using (Stream itemStream = Util.OpenFile(map[itemKey], handler)) {
                        STUD itemStud = new STUD(itemStream);
                        if (itemStud.Instances == null) {
                            continue;
                        }
                        SkinItem item = itemStud.Instances[0] as SkinItem;
                        if (item == null) {
                            continue;
                        }
                        Dictionary<ulong, List<ulong>> sound = new Dictionary<ulong, List<ulong>>();
                        if (!haveBase) {
                            sound.Clear();
                            Console.Out.WriteLine("Processing base sounds for hero {0}", heroName);
                            Skin.ExtractData(item, master, false, new HashSet<ulong>(), new Dictionary<ulong, ulong>(), new HashSet<ulong>(), new Dictionary<ulong, List<ImageLayer>>(), new Dictionary<ulong, ulong>(), sound, new List<ulong>(), -1, map, handler);
                            Sound.FindSounds(master, track, map, handler, null, masterKey, sound);
                            DumpVoice.Save($"{path}_Base{Path.DirectorySeparatorChar}", sound, map, handler, quiet, null, done);
                            haveBase = true;
                        }
                        sound.Clear();
                        string itemName = Util.GetString(item.Header.name, map, handler);
                        if (string.IsNullOrWhiteSpace(itemName)) {
                            itemName = $"Item-{itemKey:X16}";
                        }
                        Console.Out.WriteLine("Processing new sounds for skin {0}", itemName);
                        Dictionary<ulong, ulong> replace = new Dictionary<ulong, ulong>();
                        Skin.ExtractData(item, master, true, new HashSet<ulong>(), new Dictionary<ulong, ulong>(), new HashSet<ulong>(), new Dictionary<ulong, List<ImageLayer>>(), replace, sound, new List<ulong>(), -1, map, handler);
                        Sound.FindSounds(master, track, map, handler, replace, masterKey, sound);
                        DumpVoice.Save($"{path}{Util.Strip(Util.SanitizePath(itemName))}{Path.DirectorySeparatorChar}", sound, map, handler, quiet, replace, done);
                    }
                }
            }
        }
    }
}
