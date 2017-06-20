using System;
using System.Collections.Generic;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using System.IO;

namespace OverTool {
    class DumpNPC : IOvertool {
        public string Help => "output [names]";
        public uint MinimumArgs => 1;
        public char Opt => 'N';
        public string Title => "Extract NPC";
        public ushort[] Track => new ushort[1] { 0x75 };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            List<ulong> masters = track[0x75];
            List<ulong> blank = new List<ulong>();
            Dictionary<ulong, ulong> blankdict = new Dictionary<ulong, ulong>();
            List<string> extract = new List<string>();
            for (int i = 3; i < flags.Positionals.Length; ++i) {
                extract.Add(flags.Positionals[i].ToLowerInvariant());
            }
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
                if (extract.Count > 0 && !extract.Contains(heroName.ToLowerInvariant())) {
                    continue;
                }
                if (master.Header.itemMaster.key != 0) { // AI
                    InventoryMaster inventory = Extract.OpenInventoryMaster(master, map, handler);
                    if (inventory.ItemGroups.Length > 0 || inventory.DefaultGroups.Length > 0) {
                        continue;
                    }
                }
                Console.Out.WriteLine("{0} {1:X}", heroName, GUID.Index(masterKey));
                string path = string.Format("{0}{1}{2}{1}{3:X}{1}", flags.Positionals[2], Path.DirectorySeparatorChar, Util.Strip(Util.SanitizePath(heroName)), GUID.LongKey(masterKey));

                HashSet<ulong> models = new HashSet<ulong>();
                Dictionary<ulong, ulong> animList = new Dictionary<ulong, ulong>();
                HashSet<ulong> parsed = new HashSet<ulong>();
                Dictionary<ulong, List<ImageLayer>> layers = new Dictionary<ulong, List<ImageLayer>>();
                Dictionary<ulong, List<ulong>> sound = new Dictionary<ulong, List<ulong>>();

                ExtractLogic.Skin.FindModels(master.Header.binding, blank, models, animList, layers, blankdict, parsed, map, handler, sound);

                ExtractLogic.Skin.Save(master, path, heroName, $"{GUID.LongKey(masterKey):X}", blankdict, parsed, models, layers, animList, flags, track, map, handler, masterKey, false, quiet, sound, 0);
            }
        }
    }
}