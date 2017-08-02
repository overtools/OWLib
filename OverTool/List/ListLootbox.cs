using System;
using System.IO;
using System.Collections.Generic;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;

namespace OverTool.List {
    class ListLootbox : IOvertool {
        public string Help => "No additional arguments";
        public uint MinimumArgs => 0;
        public char Opt => 'l';
        public string FullOpt => "list-lootbox";
        public string Title => "List Lootbox";
        public ushort[] Track => new ushort[1] { 0xCF };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            Console.Out.WriteLine();
            foreach (ulong master in track[0xCF]) {
                if (!map.ContainsKey(master)) {
                    continue;
                }
                STUD lootbox = new STUD(Util.OpenFile(map[master], handler));
                if (lootbox.Instances == null) { continue; }
#if OUTPUT_STUDLOOTBOX
                Stream instream = Util.OpenFile(map[master], handler);
                string outFilename = string.Format("./STUD/Lootboxes/{0:X16}.mat", master);
                string putPathname = outFilename.Substring(0, outFilename.LastIndexOf('/'));
                Directory.CreateDirectory(putPathname);
                Stream OutWriter = File.Create(outFilename);
                instream.CopyTo(OutWriter);
                OutWriter.Close();
                instream.Close();
#endif
                Lootbox box = lootbox.Instances[0] as Lootbox;
                if (box == null) { continue; }
                Console.Out.WriteLine(box.EventNameNormal);
                Console.Out.WriteLine("\t{0}", Util.GetString(box.Master.title, map, handler));
                foreach (Lootbox.Bundle bundle in box.Bundles) {
                    Console.Out.WriteLine("\t\t{0}", Util.GetString(bundle.title, map, handler));
                }
            }
        }
    }
}