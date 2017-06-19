using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CASCExplorer;
using OverTool.ExtractLogic;
using OWLib;
using OWLib.Types.Map;
using OWLib.Types.STUD;

namespace OverTool {
    class DumpMapAudio : IOvertool {
        public string Help => "output [query]";
        public uint MinimumArgs => 1;
        public char Opt => 'a';
        public string Title => "Extract Map Audio";
        public ushort[] Track => new ushort[1] { 0x9F };
        public bool Display => true;
        
        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            string output = flags.Positionals[2];

            List<string> maps = new List<string>();
            if (flags.Positionals.Length > 3) {
                maps.AddRange(flags.Positionals.Skip(2).Select((it) => it.ToLower()));
            }
            bool wildcard = maps.Count == 0 || maps.Contains("*");

            List<ulong> masters = track[0x9F];
            Dictionary<ulong, ulong> replace = new Dictionary<ulong, ulong>();
            foreach (ulong masterKey in masters) {
                if (!map.ContainsKey(masterKey)) {
                    continue;
                }
                STUD masterStud = new STUD(Util.OpenFile(map[masterKey], handler));
                if (masterStud.Instances == null || masterStud.Instances[0] == null) {
                    continue;
                }
                MapMaster master = (MapMaster)masterStud.Instances[0];
                if (master == null) {
                    continue;
                }
                string name = Util.GetString(master.Header.name.key, map, handler);
                if (string.IsNullOrWhiteSpace(name)) {
                    name = $"Unknown{GUID.Index(master.Header.data.key):X}";
                }
                if (!maps.Contains(name.ToLowerInvariant()) && !wildcard) {
                    continue;
                }
                Console.Out.WriteLine("Dumping audio for map {0}", name);
                Dictionary<ulong, List<ulong>> soundData = new Dictionary<ulong, List<ulong>>();
                string path = string.Format("{0}{1}{2}{1}{3}{1}{4}{1}", output, Path.DirectorySeparatorChar, Util.Strip(Util.SanitizePath(name)), "Audio", GUID.Index(master.Header.data.key));
                HashSet<ulong> soundDone = new HashSet<ulong>();
                using (Stream mapBStream = Util.OpenFile(map[master.DataKey(0xB)], handler)) {
                    if (mapBStream != null) {
                        Map mapB = new Map(mapBStream);
                        foreach (STUD stud in mapB.STUDs) {
                            Sound.FindSoundsSTUD(stud, soundDone, soundData, map, handler, replace, masterKey, master.DataKey(0xB));
                        }

                        for (int i = 0; i < mapB.Records.Length; ++i) {
                            if (mapB.Records[i] != null && mapB.Records[i].GetType() != typeof(Map0B)) {
                                continue;
                            }
                            Map0B mapprop = (Map0B)mapB.Records[i];
                            if (!map.ContainsKey(mapprop.Header.binding)) {
                                continue;
                            }
                            Sound.FindSoundsEx(mapprop.Header.binding, soundDone, soundData, map, handler, replace, master.DataKey(0xB));
                        }
                    }
                }
                using (Stream map11Stream = Util.OpenFile(map[master.DataKey(0x11)], handler)) {
                    if (map11Stream != null) {
                        Map11 map11 = new Map11(map11Stream);
                        Sound.FindSoundsSTUD(map11.main, soundDone, soundData, map, handler, replace, masterKey, master.DataKey(0x11));
                        Sound.FindSoundsSTUD(map11.secondary, soundDone, soundData, map, handler, replace, masterKey, master.DataKey(0x11));
                    }
                }
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }

                DumpVoice.Save(path, soundData, map, handler, quiet);
            }
        }
    }
}
