using System;
using System.Collections.Generic;
using CASCExplorer;

namespace OverTool {
    public class Debug : IOvertool {
        public string Help => "No additional arguments";
        public uint MinimumArgs => 0;
        public char Opt => '~';
        public string Title => "Debug";
        public ushort[] Track => new ushort[0];

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, string[] args) {
            foreach (KeyValuePair<ushort, List<ulong>> pair in track) {
                Console.Out.WriteLine($"{pair.Key:X3} {pair.Value.Count} entries");
            }
        }
    }
}
