using System;
using System.Collections.Generic;
using System.IO;
using CASCLib;
using STULib;
using STULib.Types;
using System.Linq;

namespace OverTool.List {
    public class ListBrawlName : IOvertool {
        public string Title => "List Brawl Names";
        public char Opt => '\0';
        public string FullOpt => "list-brawlname";
        public string Help => null;
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
                    ISTU stu = ISTU.NewInstance(input, uint.MaxValue);
                    if (stu.Instances == null) {
                        continue;
                    }
                    STUBrawlName bn = stu.Instances.First() as STUBrawlName;

                    // BrawlName bn = stud.Instances[0] as BrawlName;
                    if (bn == null) {
                        continue;
                    }

                    Console.Out.WriteLine($"{key}: {Util.GetString(bn.Name, map, handler)}");
                }
            }
        }
    }
}
