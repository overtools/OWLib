using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;

namespace OverTool {
    public class DumpKey : IOvertool {
        public string Title => "List Keys";
        public char Opt => 'Z';
        public string FullOpt => "keys";
        public string Help => null;
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0x90 };
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

        private string ByteArrayToString(byte[] array) {
            string str = "";
            foreach (byte b in array) {
                str += $"{b:X2}";
            }
            return str;
        }

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
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

            if (System.Diagnostics.Debugger.IsAttached) {
                using (Stream output = File.Open("ow_dump.keys", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)) {
                    using (TextWriter writer = new StreamWriter(output)) {
                        foreach (KeyValuePair<ulong, byte[]> key in KeyService.keys) {
                            writer.WriteLine("{0:X16} {1}", key.Key, ByteArrayToString(key.Value));
                        }
                    }
                }
                if (handler.Config.KeyRing != null) {
                    using (Stream output = File.Open("ow_keyring.keys", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)) {
                        using (TextWriter writer = new StreamWriter(output)) {
                            foreach (KeyValuePair<string, List<string>> pair in handler.Config.KeyRing.KeyValue) {
                                if (pair.Key.StartsWith("key-")) {
                                    string reverseKey = pair.Key.Substring(pair.Key.Length - 16);
                                    string key = "";
                                    for (int i = 0; i < 8; ++i) {
                                        key = reverseKey.Substring(i * 2, 2) + key;
                                    }
                                    ulong keyL = ulong.Parse(key, System.Globalization.NumberStyles.HexNumber);
                                    writer.WriteLine("{0:X16} {1}", keyL, pair.Value[0].ToByteArray());
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
