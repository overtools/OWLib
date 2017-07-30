using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;

namespace OverTool.List {
    public class ListGameMode : IOvertool {
        public string Title => "List Game Modes";
        public char Opt => 'E';
        public string Help => "No additional arguments";
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0xC0 };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (ulong key in track[0xC0]) {
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

                    GameMode gm = stud.Instances[0] as GameMode;
                    if (gm == null) {
                        continue;
                    }

                    string name = Util.GetString(gm.Header.name, map, handler);
                    if (name != null) {
                        Console.Out.WriteLine("{0:X8}: {1}", GUID.Index(gm.Header.ruleset), name);
                    }
                }
            }
        }
    }
}
