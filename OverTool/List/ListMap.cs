using System;
using System.Collections.Generic;
using CASCExplorer;
using STULib;
using STULib.Types;
using System.Linq;

namespace OverTool {
    class ListMap : IOvertool {
        public string Help => null;
        public uint MinimumArgs => 0;
        public char Opt => 'm';
        public string FullOpt => "list-map";
        public string Title => "List Maps";
        public ushort[] Track => new ushort[1] { 0x9F };
        public bool Display => true;

        public static void TryString(string n, string str) {
            if (str == null) {
                return;
            } else {
                Console.Out.WriteLine("\t{0}: {1}", n, str);
            }
        }

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            List<ulong> masters = track[0x9F];
            foreach (ulong key in masters) {
                if (!map.ContainsKey(key)) {
                    continue;
                }
                ISTU stu = ISTU.NewInstance(Util.OpenFile(map[key], handler), uint.MaxValue);
                if (stu.Instances == null) {
                    continue;
                }
                STUMap mapSTU = stu.Instances.First() as STUMap;
                if (map == null) {
                    continue;
                }
                Console.Out.WriteLine($"{key}");

                if (mapSTU.StringDescriptionA != null) { 
                    Console.Out.WriteLine($"\tStringDescriptionA: {Util.GetString(mapSTU.StringDescriptionA, map, handler)}");
                }
                if (mapSTU.StringStateA != null) {
                    Console.Out.WriteLine($"\tStringStateA: {Util.GetString(mapSTU.StringStateA, map, handler)}");
                }
                if (mapSTU.StringName != null) {
                    Console.Out.WriteLine($"\tStringName: {Util.GetString(mapSTU.StringName, map, handler)}");
                }
                if (mapSTU.StringStateB != null) {
                    Console.Out.WriteLine($"StringStateB: {Util.GetString(mapSTU.StringStateB, map, handler)}");
                }
                if (mapSTU.StringDescriptionB != null) {
                    Console.Out.WriteLine($"\tStringDescriptionB: {Util.GetString(mapSTU.StringDescriptionB, map, handler)}");
                }
                if (mapSTU.StringSubline != null) {
                    Console.Out.WriteLine($"\tStringSubline: {Util.GetString(mapSTU.StringSubline, map, handler)}");
                }
                if (mapSTU.StringNameB != null) {
                    Console.Out.WriteLine($"\tStringNameB: {Util.GetString(mapSTU.StringNameB, map, handler)}");
                }

                //string name = Util.GetString(map.Header.name.key, map, handler);
                //if (string.IsNullOrWhiteSpace(name)) {
                //    name = $"Unknown{GUID.Index(masterKey):X}";
                //}
                //Console.Out.WriteLine(name);
                //Console.Out.WriteLine("\tID: {0:X8}", GUID.Index(masterKey));

                //string subline = Util.GetString(map.Header.subline.key, map, handler);
                //if (subline == null) {
                //    subline = "";
                //}
                //if (map.Header.subline.key != map.Header.subline2.key && map.Header.subline2.key != 0) {
                //    subline += " " + Util.GetString(map.Header.subline2.key, map, handler);
                //}
                //subline = subline.Trim();
                //if (subline.Length > 0) {
                //    Console.Out.WriteLine("\tSubline: {0}", subline);
                //}

                //TryString("State", Util.GetString(map.Header.stateA.key, map, handler));
                //TryString("State", Util.GetString(map.Header.stateB.key, map, handler));

                //string description = Util.GetString(map.Header.description1.key, map, handler);
                //if (description == null) {
                //    description = "";
                //}
                //if (map.Header.description1.key != map.Header.description2.key && map.Header.description2.key != 0) {
                //    description += " " + Util.GetString(map.Header.description2.key, map, handler);
                //}
                //description = description.Trim();
                //if (description.Length > 0) {
                //    Console.Out.WriteLine("\tDescription: {0}", description);
                //}
            }
        }
    }
}
