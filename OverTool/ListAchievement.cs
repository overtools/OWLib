using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
  class ListAchievement {
    public static void TryOutput(ulong key, Dictionary<ulong, Record> map, CASCHandler handler, string desc) {
      if(!map.ContainsKey(key) || key == 0) {
        return;
      }
      string str = Util.GetString(key, map, handler);
      if(!string.IsNullOrEmpty(str)) {
        Console.Out.WriteLine("\t{0}: {1}", desc, str);
      }
    }

    public static void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
      foreach(ulong key in track[0x68]) {
        if(!map.ContainsKey(key)) {
          continue;
        }
        using(Stream input = Util.OpenFile(map[key], handler)) {
          if(input == null) {
            continue;
          }
          STUD stud = new STUD(input);
          if(stud.Instances == null || stud.Instances[0] == null) {
            continue;
          }
          Achievement achievement = stud.Instances[0] as Achievement;
          if(achievement == null) {
            continue;
          }

          Console.Out.WriteLine("{0:X8}:", APM.keyToIndex(key));
          TryOutput(achievement.Data.name, map, handler, "Name");
          TryOutput(achievement.Data.description1, map, handler, "Description 1");
          TryOutput(achievement.Data.description2, map, handler, "Description 2");
          if(achievement.Data.reward != 0) {
            using(Stream inputReward = Util.OpenFile(map[achievement.Data.reward], handler)) {
              if(inputReward != null) {
                STUD reward = new STUD(inputReward);
                if(reward.Instances != null && reward.Instances[0] != null) {
                  IInventorySTUDInstance instance = reward.Instances[0] as IInventorySTUDInstance;
                  if(instance != null) {
                    Console.Out.WriteLine("\tReward: {0} ({1} {2})", achievement.Data.reward.ToStringA(), instance.Header.rarity, reward.Instances[0].Name);
                  }
                }
              }
            }
          }
          Console.Out.WriteLine("\tIcon: {0}", achievement.Data.icon.ToStringA());
          if(achievement.Data.image != 0) {
            Console.Out.WriteLine("\tImage: {0}", achievement.Data.image.ToStringA());
          }
          Console.Out.WriteLine();
        }
      }
    }
  }
}
