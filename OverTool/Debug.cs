using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;

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

            OwRootHandler root = handler.Root as OwRootHandler;

            foreach (APMFile apm in root.APMFiles) {
                Console.Out.WriteLine(apm.Name);
                foreach (CMFEntry entry in apm.CMFEntries) {
                    Console.Out.WriteLine("{0:X8} {1:X8} {2:X8}", entry.hashA, entry.hashB, entry.Index);
                }

                Console.Out.WriteLine("--");

                foreach (APMEntry entry in apm.Entries) {
                    Console.Out.WriteLine("{0:X8} {1:X8} {2:X8}", entry.hashA, entry.hashB, entry.Index);
                }

                Console.Out.WriteLine("--");
            }
        }
    }
}
