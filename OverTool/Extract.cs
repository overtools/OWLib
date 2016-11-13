using System;
using System.Collections.Generic;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
  class Extract {
    public static void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
      if(args.Length < 1) {
        Console.Out.WriteLine("Usage: OverTool.exe overwatch x output [types [query [opts]]]");
        return;
      }

      string output = args[0];

      bool typeWildcard = true;
      List<string> types = new List<string>();
      if(args.Length > 1) {
        types.AddRange(args[1].ToLowerInvariant().Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries));
        typeWildcard = types.Contains("*");
      }
      Dictionary<string, List<string>> heroTypes = new Dictionary<string, List<string>>();
      Dictionary<string, bool> heroWildcard = new Dictionary<string, bool>();
      Dictionary<string, Dictionary<string, List<ulong>>> heroIgnore = new Dictionary<string, Dictionary<string, List<ulong>>>();
      bool heroAllWildcard = false;
      if(args.Length > 2 && args[2] != "*") {
        foreach(string pair in args[2].ToLowerInvariant().Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries)) {
          List<string> data = new List<string>(pair.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries));
          string name = data[0];
          data.RemoveAt(0);

          if(!heroTypes.ContainsKey(name)) {
            heroTypes[name] = new List<string>();
            heroIgnore[name] = new Dictionary<string, List<ulong>>();
            heroWildcard[name] = false;
          }

          if(data.Count > 0) {
            foreach(string d in data) {
              List<string> subdata = new List<string>(d.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
              string subn = subdata[0];
              subdata.RemoveAt(0);
              heroTypes[name].Add(subn);
              heroIgnore[name][subn] = new List<ulong>();
              if(subdata.Count > 0) {
                foreach(string sd in subdata) {
                  try {
                    heroIgnore[name][subn].Add(ulong.Parse(sd, System.Globalization.NumberStyles.HexNumber));
                  } catch { }
                }
              }
            }
          }

          if(data.Count == 0 || data.Contains("*")) {
            heroWildcard[name] = true;
          }
        }
      } else {
        heroAllWildcard = true;
      }

      List<char> furtherOpts = new List<char>();
      if(args.Length > 3) {
        furtherOpts.AddRange(args[3].ToCharArray());
      }

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
        if(heroAllWildcard) {
          heroTypes.Add(heroName.ToLowerInvariant(), new List<string>());
          heroWildcard.Add(heroName.ToLowerInvariant(), true);
        }
        if(!heroTypes.ContainsKey(heroName.ToLowerInvariant())) {
          continue;
        }
        if(!map.ContainsKey(master.Header.itemMaster.key)) {
          continue;
        }
        STUD inventoryStud = new STUD(Util.OpenFile(map[master.Header.itemMaster.key], handler));
        InventoryMaster inventory = (InventoryMaster)inventoryStud.Instances[0];
        if(inventory == null) {
          continue;
        }

        Dictionary<OWRecord, string> items = new Dictionary<OWRecord, string>();
        foreach(OWRecord record in inventory.Achievables) {
          items[record] = "ACHIEVEMENT";
        }

        for(int i = 0; i < inventory.DefaultGroups.Length; ++i) {
          string name = "STANDARD_"+OWLib.Util.GetEnumName(typeof(InventoryMaster.EVENT_ID), inventory.DefaultGroups[i].@event);
          for(int j = 0; j < inventory.Defaults[i].Length; ++j) {
            items[inventory.Defaults[i][j]] = name;
          }
        }

        for(int i = 0; i < inventory.ItemGroups.Length; ++i) {
          string name = OWLib.Util.GetEnumName(typeof(InventoryMaster.EVENT_ID), inventory.ItemGroups[i].@event);
          for(int j = 0; j < inventory.Items[i].Length; ++j) {
            items[inventory.Items[i][j]] = name;
          }
        }

        foreach(KeyValuePair<OWRecord, string> recordname in items) {
          OWRecord record = recordname.Key;
          string itemGroup = recordname.Value;
          if(!map.ContainsKey(record.key)) {
            continue;
          }

          STUD stud = new STUD(Util.OpenFile(map[record.key], handler));
          if(stud.Instances == null) {
            continue;
          }
          IInventorySTUDInstance instance = (IInventorySTUDInstance)stud.Instances[0];
          if(!typeWildcard && !types.Contains(instance.Name.ToLowerInvariant())) {
            continue;
          }

          string name = Util.GetString(instance.Header.name.key, map, handler);
          if(name == null) {
            continue;
          }
          if(!heroWildcard[heroName.ToLowerInvariant()] && !heroTypes[heroName.ToLowerInvariant()].Contains(name.ToLowerInvariant())) {
            continue;
          }

          switch(instance.Name) {
            case "Spray":
              Console.Out.WriteLine("Extracting spray {0} for {1}...", name, heroName);
              ExtractLogic.Spray.Extract(stud, output, heroName, name, itemGroup, track, map, handler, furtherOpts);
              break;
            case "Skin":
              List<ulong> ignoreList = new List<ulong>();
              try {
                ignoreList = heroIgnore[heroName.ToLowerInvariant()][name.ToLowerInvariant()];
              } catch { }
              Console.Out.WriteLine("Extracting {0} models and textures for {1}", name, heroName);
              ExtractLogic.Skin.Extract(master, stud, output, heroName, name, itemGroup, ignoreList, track, map, handler, furtherOpts);
              break;
            case "Icon":
              Console.Out.WriteLine("Extracting icon {0} for {1}...", name, heroName);
              ExtractLogic.Icon.Extract(stud, output, heroName, name, itemGroup, track, map, handler, furtherOpts);
              break;
            case "Voice Line":
              //Console.Out.WriteLine("Extracting voice line {0} for {1}...", name, heroName);
              //ExtractLogic.VoiceLine.Extract(master, stud, output, heroName, name, track, map, handler);
              break;
          }
        }
      }
    }
  }
}
