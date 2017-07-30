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
            Dictionary<uint, Dictionary<uint, string>> data = new Dictionary<uint, Dictionary<uint, string>>();
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
                        uint ruleset = GUID.Index(gm.Header.ruleset);
                        if (!data.ContainsKey(ruleset)) {
                            data[ruleset] = new Dictionary<uint, string>();
                        }
                        data[ruleset].Add(GUID.Index(key), name);
                    }
                }
            }
            foreach (KeyValuePair<uint, Dictionary<uint, string>> pairs in data) {
                Console.Out.WriteLine("{0:X8}:", pairs.Key);
                foreach (KeyValuePair<uint, string> kv in pairs.Value) {
                    Console.Out.WriteLine("\t{0} ({1:X8})", kv.Value, kv.Key);
                }
            }
        }
    }
}
