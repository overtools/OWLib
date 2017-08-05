using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;

namespace OverTool.List {
    public class ListBrawlName : IOvertool {
        public string Title => "List Brawl Names";
        public char Opt => ' ';
        public string FullOpt => "list-brawlname";
        public string Help => "No additional arguments";
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0xD9 };
        public bool Display => true;


        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (ulong key in track[0xD9]) {
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

                    BrawlName bn = stud.Instances[0] as BrawlName;
                    if (bn == null) {
                        continue;
                    }

                    Console.Out.WriteLine($"{key}: {Util.GetString(bn.Data.name, map, handler)}");
                }
            }
        }
    }
}
