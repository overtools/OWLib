using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
    public class Debug : IOvertool {
        public string Help => "No additional arguments";
        public uint MinimumArgs => 0;
        public char Opt => '~';
        public string Title => "Debug";
        public ushort[] Track => new ushort[0];
        public bool Display => System.Diagnostics.Debugger.IsAttached;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (KeyValuePair<ushort, List<ulong>> pair in track) {
                Console.Out.WriteLine($"{pair.Key:X3} {pair.Value.Count} entries");
            }
            /*
            // Code for rapidly finding what zero values are.
            foreach (ulong key in track[0xA5]) {
                using (Stream stream = Util.OpenFile(map[key], handler)) {
                    STUD stud = new STUD(stream);
                    if (stud.Instances == null) {
                        continue;
                    }

                    IInventorySTUDInstance instance = stud.Instances[0] as IInventorySTUDInstance;
                    if (instance == null) {
                        continue;
                    }
                    if (instance.Header.unk1.key != 0) {
                        System.Diagnostics.Debugger.Break();
                    }
                }
            }
            */
        }
    }
}
