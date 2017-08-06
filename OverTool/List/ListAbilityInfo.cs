using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;

namespace OverTool.List {
    public class ListAbilityInfo : IOvertool {
        public string Title => "List Abilities";
        public char Opt => ' ';
        public string FullOpt => "list-abilities";
        public string Help => "No additional arguments";
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0x9E };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (ulong key in track[0x9E]) {
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

                    AbilityInfo bn = stud.Instances[0] as AbilityInfo;
                    if (bn == null) {
                        continue;
                    }
                    Console.Out.WriteLine(Util.GetString(bn.Data.name, map, handler));
                    string description = Util.GetString(bn.Data.description, map, handler);
                    if (description != null) {
                        Console.Out.WriteLine("\t{0}", description);
                    }
                }
            }
        }
    }
}
