using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
  class ListInventory {
    public static void GetInventoryName(ulong key, Dictionary<ulong, Record> map, CASCHandler handler) {
      if(!map.ContainsKey(key)) {
        return;
      }

      STUD stud = new STUD(Util.OpenFile(map[key], handler));
      IInventorySTUDInstance instance = (IInventorySTUDInstance)stud.Instances[0];
      if(instance == null) {
        return;
      }

      string name = Util.GetString(instance.Header.name.key, map, handler);
      if(name == null) {
        return;
      }

      Console.Out.WriteLine("\t{0} ({1} {2})", name, instance.Header.rarity, stud.Instances[0].Name);
    }

    public static void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] v) {
      List<ulong> masters = track[0x75];
      foreach(ulong masterKey in masters) {
        if(!map.ContainsKey(masterKey)) {
          continue;
        }
        STUD masterStud = new STUD(Util.OpenFile(map[masterKey], handler));
        HeroMaster master = (HeroMaster)masterStud.Instances[0];
        if(master == null) {
          continue;
        }
        string heroName = Util.GetString(master.Header.name.key, map, handler);
        if(heroName == null) {
          continue;
        }
        Console.Out.WriteLine("Cosmetics for {0}...", heroName);
        if(!map.ContainsKey(master.Header.itemMaster.key)) {
          Console.Out.WriteLine("Error loading inventory master file...");
          continue;
        }
        STUD inventoryStud = new STUD(Util.OpenFile(map[master.Header.itemMaster.key], handler));
        InventoryMaster inventory = (InventoryMaster)inventoryStud.Instances[0];
        if(inventory == null) {
          Console.Out.WriteLine("Error loading inventory master file...");
          continue;
        }

        foreach(OWRecord record in inventory.Achievables) {
          GetInventoryName(record.key, map, handler);
        }

        foreach(OWRecord[] records in inventory.Defaults) {
          foreach(OWRecord record in records) {
            GetInventoryName(record.key, map, handler);
          }
        }

        foreach(OWRecord[] records in inventory.Items) {
          foreach(OWRecord record in records) {
            GetInventoryName(record.key, map, handler);
          }
        }
        Console.Out.WriteLine("");
      }
    }
  }
}
