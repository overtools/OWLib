using System;
using System.Collections.Generic;
using System.IO;
using CASCLib;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;

namespace OverTool.List {
    public class ListGameMode : IOvertool {
        public string Title => "List Game Modes";
        public char Opt => 'e';
        public string FullOpt => "list-gamemode";
        public string Help => null;
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0xC7 };
        public bool Display => true;

        private string GetGameType(ulong key, Dictionary<ulong, Record> map, CASCHandler handler) {
            if (!map.ContainsKey(key)) {
                return null;
            }

            using (Stream input = Util.OpenFile(map[key], handler)) {
                if (input == null) {
                    return null;
                }
                STUD stud = new STUD(input);
                if (stud.Instances == null || stud.Instances[0] == null) {
                    return null;
                }

                GameType gm = stud.Instances[0] as GameType;
                if (gm == null) {
                    return null;
                }

                return Util.GetString(gm.Header.name, map, handler);
            }
        }

        private string GetGameParam(ulong key, Dictionary<ulong, Record> map, CASCHandler handler) {
            if (!map.ContainsKey(key)) {
                return null;
            }

            using (Stream input = Util.OpenFile(map[key], handler)) {
                if (input == null) {
                    return null;
                }
                STUD stud = new STUD(input);
                if (stud.Instances == null || stud.Instances[0] == null) {
                    return null;
                }

                GameTypeParam gm = stud.Instances[0] as GameTypeParam;
                if (gm == null) {
                    return null;
                }

                return Util.GetString(gm.Header.name, map, handler);
            }
        }

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (ulong key in track[0xC7]) {
                if (!map.ContainsKey(key)) {
                    continue;
                }
                using (Stream input = Util.OpenFile(map[key], handler)) {
                    if (input == null) {
                        continue;
                    }
                    STUD stud = new STUD(input);
                    if (stud.Instances == null || stud.Instances[0] == null) {
                        continue;
                    }

                    GameMode gm = stud.Instances[0] as GameMode;
                    if (gm == null) {
                        continue;
                    }

                    string name = Util.GetString(gm.Header.name, map, handler);
                    if (name != null) {
                        string desc = Util.GetString(gm.Header.description, map, handler);
                        string difficultyName = Util.GetString(gm.Header.difficultyName, map, handler);
                        string difficultyDescription = Util.GetString(gm.Header.difficultyDescription, map, handler);
                        Console.Out.WriteLine(name);

                        if (difficultyName != null) {
                            Console.Out.WriteLine("\t{0}", difficultyName);
                        }

                        if (difficultyDescription != null) {
                            Console.Out.WriteLine("\t{0}", difficultyDescription);
                        }

                        if (desc != null) {
                            Console.Out.WriteLine("\t{0}", desc);
                        }
                        foreach (OWRecord @string in gm.Strings) {
                            string str = Util.GetString(@string, map, handler);
                            if (str != null) {
                                Console.Out.WriteLine("\t{0}", str);
                            }
                        }
                        foreach (OWRecord gp in gm.Params) {
                            string str = GetGameParam(gp, map, handler);
                            if (str != null) {
                                Console.Out.WriteLine("\t{0}", str);
                            }
                        }
                        foreach (OWRecord gt in gm.Types) {
                            string str = GetGameType(gt, map, handler);
                            if (str != null) {
                                Console.Out.WriteLine("\t{0}", str);
                            }
                        }
                    }
                }
            }
        }
    }
}
