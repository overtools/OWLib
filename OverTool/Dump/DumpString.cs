using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;

namespace OverTool {
    public class DumpString : IOvertool {
        public string Help => "No additional arguments";
        public uint MinimumArgs => 0;
        public char Opt => 's';
        public string FullOpt => "strings";
        public string Title => "List Strings";
        public ushort[] Track => new ushort[2] { 0x7C, 0xA9 };
        public bool Display => true;

        public static void Iterate(List<ulong> files, Dictionary<ulong, Record> map, CASCHandler handler) {
            foreach (ulong key in files) {
                if (!map.ContainsKey(key)) {
                    continue;
                }
                using (Stream stream = Util.OpenFile(map[key], handler)) {
                    if (stream == null) {
                        continue;
                    }
                    try {
                        OWString str = new OWString(stream);
                        if (str.Value == null || str.Value.Length == 0) {
                            continue;
                        }
                        Console.Out.WriteLine("{0:X12}.{1:X3}: {2}", GUID.LongKey(key), GUID.Type(key), str.Value);
                    } catch {
                        Console.Out.WriteLine("Error with file {0:X12}.{1:X3}", GUID.LongKey(key), GUID.Type(key));
                    }
                }
            }
        }

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            Iterate(track[0x7C], map, handler);
            Iterate(track[0xA9], map, handler);
        }
    }
}
