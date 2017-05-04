using System;
using System.Collections.Generic;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;

namespace OverTool {
    class ListNPC : IOvertool {
        public string Help => "No additional arguments";
        public uint MinimumArgs => 0;
        public char Opt => 'n';
        public string Title => "List NPCs";
        public ushort[] Track => new ushort[1] { 0x75 };

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
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
                if (master.Header.itemMaster.key != 0) { // AI
                    continue;
                }
                Console.Out.WriteLine("{0} {1:X}", heroName, GUID.LongKey(masterKey));
            }
        }
    }
}
