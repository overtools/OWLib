using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;

namespace OverTool {
    public class DumpKey : IOvertool {
        public string Title => "List Keys";
        public char Opt => 'Z';
        public string Help => "No additional arguments";
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0x90 };

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

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] opts) {
            Console.Out.WriteLine("key_name          key");
            foreach (ulong key in track[0x90]) {
                if (!map.ContainsKey(key)) {
                    continue;
                }
                using (Stream stream = Util.OpenFile(map[key], handler)) {
                    if (stream == null) {
                        continue;
                    }
                    STUD stud = new STUD(stream);
                    if (stud.Instances[0].Name != stud.Manager.GetName(typeof(EncryptionKey))) {
                        continue;
                    }
                    EncryptionKey ek = (EncryptionKey)stud.Instances[0];
                    Console.Out.WriteLine("{0}  {1}", ek.KeyNameText, ek.KeyValueText);
                }
            }
        }
    }
}
