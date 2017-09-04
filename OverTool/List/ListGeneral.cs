using System;
using System.Collections.Generic;
using System.IO;
using CASCLib;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;

namespace OverTool {
    public class ListGeneral : IOvertool {
        public string Title => "List General Cosmetics";
        public char Opt => 'g';
        public string FullOpt => "list-general";
        public string Help => null;
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0x54 };
        public bool Display => true;
        
        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (ulong key in track[0x54]) {
                if (!map.ContainsKey(key)) {
                    continue;
                }
                Dictionary<OWRecord, string> items = new Dictionary<OWRecord, string>();
                using (Stream input = Util.OpenFile(map[key], handler)) {
                    if (input == null) {
                        continue;
                    }
                    STUD stud = new STUD(input);
                    if (stud.Instances == null || stud.Instances[0] == null) {
                        continue;
                    }

                    GlobalInventoryMaster master = stud.Instances[0] as GlobalInventoryMaster;
                    if (master == null) {
                        continue;
                    }
                    
                    Console.Out.WriteLine("\tACHIEVEMENT");
                    foreach (OWRecord record in master.StandardItems) {
                        ListInventory.GetInventoryName(record.key, false, map, handler, "General");
                    }

                    Dictionary<string, List<OWRecord>> aggreg = new Dictionary<string, List<OWRecord>>();
                    for (int i = 0; i < master.Generic.Length; ++i) {
                        if (master.GenericItems[i].Length == 0) {
                            continue;
                        }
                        string s = $"\tSTANDARD_{ItemEvents.GetInstance().GetEvent(master.Generic[i].@event)}";
                        if (!aggreg.ContainsKey(s)) {
                            aggreg[s] = new List<OWRecord>();
                        }
                        for (int j = 0; j < master.GenericItems[i].Length; ++j) {
                            aggreg[s].Add(master.GenericItems[i][j]);
                        }
                    }

                    foreach (KeyValuePair<string, List<OWRecord>> pair in aggreg) {
                        Console.Out.WriteLine(pair.Key);
                        foreach (OWRecord record in pair.Value) {
                            ListInventory.GetInventoryName(record, false, map, handler, "General");
                        }
                    }

                    aggreg.Clear();

                    for (int i = 0; i < master.Categories.Length; ++i) {
                        if (master.CategoryItems[i].Length == 0) {
                            continue;
                        }
                        Console.Out.WriteLine($"\t{ItemEvents.GetInstance().GetEvent(master.Categories[i].@event)}");
                        for (int j = 0; j < master.CategoryItems[i].Length; ++j) {
                            ListInventory.GetInventoryName(master.CategoryItems[i][j], false, map, handler, "General");
                        }
                    }

                    for (int i = 0; i < master.ExclusiveOffsets.Length; ++i) {
                        if (master.LootboxExclusive[i].Length == 0) {
                            continue;
                        }
                        Console.Out.WriteLine($"\tLOOTBOX_EXCLUSIVE_{ItemEvents.GetInstance().GetEvent((ulong)i)}");
                        for (int j = 0; j < master.LootboxExclusive[i].Length; ++j) {
                            ListInventory.GetInventoryName(master.LootboxExclusive[i][j].item, false, map, handler, "General");
                        }
                    }
                }
            }
        }
    }
}
