using System;
using System.Collections.Generic;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
  class ListInventory {
    public static void GetInventoryName(ulong key, bool ex, Dictionary<ulong, Record> map, CASCHandler handler) {
      if(!map.ContainsKey(key)) {
        return;
      }

      STUD stud = new STUD(Util.OpenFile(map[key], handler));
      if(stud.Instances == null) {
        return;
      }
      if(stud.Instances[0] == null) {
        return;
      }
      IInventorySTUDInstance instance = (IInventorySTUDInstance)stud.Instances[0];
      if(instance == null) {
        return;
      }

      string name = Util.GetString(instance.Header.name.key, map, handler);
      if(name == null) {
        return;
      }

      if(ex) {
        Console.Out.WriteLine("\t\t{0} ({1} {2} in package {3:X16})", name, instance.Header.rarity, stud.Instances[0].Name, map[key].package.packageKey);
      } else {
        Console.Out.WriteLine("\t\t{0} ({1} {2})", name, instance.Header.rarity, stud.Instances[0].Name);
      }
    }

    public static void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
      List<ulong> masters = track[0x75];
      foreach(ulong masterKey in masters) {
        if(!map.ContainsKey(masterKey)) {
          continue;
        }
        STUD masterStud = new STUD(Util.OpenFile(map[masterKey], handler));
        if(masterStud.Instances == null) {
          continue;
        }
        HeroMaster master = (HeroMaster)masterStud.Instances[0];
        if(master == null) {
          continue;
        }
        string heroName = Util.GetString(master.Header.name.key, map, handler);
        if(heroName == null) {
          continue;
        }
        if(master.Header.itemMaster.key == 0) { // AI
          continue;
        }
        bool ex = System.Diagnostics.Debugger.IsAttached;
        List<string> largs = new List<string>(args);
        if(largs.Count > 0 && largs.Contains("ex")) {
          ex = true;
        }
        if(ex) {
          Console.Out.WriteLine("Cosmetics for {0} in package {1:X16}", heroName, map[masterKey].package.packageKey);
        } else {
          Console.Out.WriteLine("Cosmetics for {0}", heroName);
        }
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

        Console.Out.WriteLine("\tACHIEVEMENT ({0} items)", inventory.Achievables.Length);
        foreach(OWRecord record in inventory.Achievables) {
          GetInventoryName(record.key, ex, map, handler);
        }

        for(int i = 0; i < inventory.DefaultGroups.Length; ++i) {
          if(inventory.Defaults[i].Length == 0) {
            continue;
          }
          OWRecord[] records = inventory.Defaults[i];
          Console.Out.WriteLine("\tSTANDARD_{0} ({1} items)", ItemEvents.GetInstance().GetEvent(inventory.DefaultGroups[i].@event), records.Length);
          foreach(OWRecord record in records) {
            GetInventoryName(record.key, ex, map, handler);
          }
        }

        for(int i = 0; i < inventory.ItemGroups.Length; ++i) {
          if(inventory.Items[i].Length == 0) {
            continue;
          }
          OWRecord[] records = inventory.Items[i];
          Console.Out.WriteLine("\t{0} ({1} items)", ItemEvents.GetInstance().GetEvent(inventory.ItemGroups[i].@event), records.Length);
          foreach(OWRecord record in records) {
            GetInventoryName(record.key, ex, map, handler);
          }
        }
        Console.Out.WriteLine("");
      }
    }
  }
}
