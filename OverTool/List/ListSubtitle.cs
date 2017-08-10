using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;

namespace OverTool.List {
    public class ListSubtitle : IOvertool {
        public string Title => "List Subtitles";
        public char Opt => '\0';
        public string FullOpt => "list-subtitle";
        public string Help => null;
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0x71 };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (ulong key in track[0x71]) {
                if (!map.ContainsKey(key)) {
                    continue;
                }
                using (Stream input = Util.OpenFile(map[key], handler)) {
                    if (input == null) {
                        continue;
                    }
                    STUD stud = new STUD(input);
                    if (stud.Instances == null || stud.Instances.Length < 1 || stud.Instances[1] == null) {
                        continue;
                    }

                    Subtitle st = stud.Instances[1] as Subtitle;
                    if (st == null) {
                        continue;
                    }

                    Console.Out.WriteLine($"{key}: {st.String}");
                }
            }
        }
    }
}
