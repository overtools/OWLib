using System;
using System.Collections.Generic;
using CASCExplorer;

namespace OverTool {
    public class Debug : IOvertool {
        public string Help => null;
        public uint MinimumArgs => 0;
        public char Opt => '~';
        public string FullOpt => "debug";
        public string Title => "Debug";
        public ushort[] Track => null;
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (KeyValuePair<ushort, List<ulong>> pair in track) {
                Console.Out.WriteLine($"{pair.Key:X3} {pair.Value.Count} entries");
            }
        }
    }
}
