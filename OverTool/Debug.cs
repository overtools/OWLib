using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;

namespace OverTool {
    public class Debug : IOvertool {
        public string Help => "No additional arguments";
        public uint MinimumArgs => 0;
        public char Opt => '~';
        public string Title => "Debug";
        public ushort[] Track => new ushort[1] { 0xA6 };
        public bool Display => System.Diagnostics.Debugger.IsAttached;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (KeyValuePair<ushort, List<ulong>> pair in track) {
                Console.Out.WriteLine($"{pair.Key:X3} {pair.Value.Count} entries");
            }
            // Code for rapidly finding what zero values are.
            foreach (ulong key in track[0xA6]) {
                using (Stream stream = Util.OpenFile(map[key], handler)) {
                    STUD stud = new STUD(stream);
                    if (stud.Instances == null) {
                        continue;
                    }

                    if (stud.Instances[0] is TextureOverride @override) {
                        if (@override.Header.unknown2 > 0 || @override.Header.unknown4 > 0 || @override.Header.unknown7 > 0 || @override.Header.unknown12.key > 0 || @override.Header.unknown13 > 0) {
                            System.Diagnostics.Debugger.Break();
                        }
                    }
                }
            }
        }
    }
}
