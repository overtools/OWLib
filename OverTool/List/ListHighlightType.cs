using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;

namespace OverTool.List {
    public class ListHighlightType : IOvertool {
        public string Title => "List Highlight Types";
        public char Opt => '\0';
        public string FullOpt => "list-highlighttype";
        public string Help => null;
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0xC2 };
        public bool Display => true;


        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (ulong key in track[0xC2]) {
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

                    HighlightType ht = stud.Instances[0] as HighlightType;
                    if (ht == null) {
                        continue;
                    }

                    Console.Out.WriteLine($"{key}: {Util.GetString(ht.Data.name, map, handler)}");
                }
            }
        }
    }
}
