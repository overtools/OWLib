using System;
using System.Collections.Generic;
using CASCLib;
using OWLib;

namespace OverTool {
    public class FileTypes : IOvertool {
        public string Title => "File Types";
        public char Opt => '\0';
        public string FullOpt => "file-types";
        public string Help => "Output file types";
        public uint MinimumArgs => 0;
        public ushort[] Track => null;
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            Console.Out.WriteLine(" BE  :  LE  : SWP");
            Dictionary<ushort, ushort> types = new Dictionary<ushort, ushort>();

            foreach (ulong key in map.Keys) {
                ushort normal = (ushort)(key >> 48);
                ushort guid = GUID.Type(key);
                if (!types.ContainsKey(normal)) {
                    types[normal] = guid;
                }
            }

            foreach (KeyValuePair<ushort, ushort> type in types) {
                ushort be = type.Key;
                ushort le = (ushort)(((be & 0xFF) << 8) + ((be & 0xFF00) >> 8));
                ushort swp = type.Value;

                Console.Out.WriteLine("{0:X4} : {1:X4} : {2:X3}", le, be, swp);
            }
        }
    }
}
