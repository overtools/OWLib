using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;

namespace OverTool.List {
    public class ListUnknownE76D9EC1 : IOvertool {
        public string Title => "List UnknownE76D9EC1";
        public char Opt => '\0';
        public string FullOpt => "list-UnknownE76D9EC1";
        public string Help => null;
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0x1F };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (ulong key in track[0x1F]) {
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

                    UnknownE76D9EC1 bn = stud.Instances[0] as UnknownE76D9EC1;
                    if (bn == null) {
                        continue;
                    }

                    Console.Out.WriteLine($"{key}");
                    foreach (OWRecord record in bn.Records) {
                        Console.Out.WriteLine($"\t{record.key}");
                    }
                }
            }
        }
    }
}
