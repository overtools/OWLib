using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
  public class DumpAchievement {
    private static void ExtractImage(ulong imageKey, string dpath, Dictionary<ulong, Record> map, CASCHandler handler, string name = null) {
      ulong imageDataKey = (imageKey & 0xFFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL;
      if(string.IsNullOrWhiteSpace(name)) {
        name = $"{APM.keyToIndexID(imageKey):X12}";
      }
      string path = $"{dpath}{name}.dds";
      if(!Directory.Exists(Path.GetDirectoryName(path))) {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
      }
      using(Stream outp = File.Open(path, FileMode.Create, FileAccess.Write)) {
        if(map.ContainsKey(imageDataKey)) {
          Texture tex = new Texture(Util.OpenFile(map[imageKey], handler), Util.OpenFile(map[imageDataKey], handler));
          tex.Save(outp);
        } else {
          TextureLinear tex = new TextureLinear(Util.OpenFile(map[imageKey], handler));
          tex.Save(outp);
        }
      }

      Console.Out.WriteLine("Wrote image {0}", path);
    }

    public static void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
      if(args.Length < 1) {
        Console.Out.WriteLine("Usage: OverTool.exe overwatch A output");
      }
      string output = args[0];
      foreach(ulong key in track[0x68]) {
        if(!map.ContainsKey(key)) {
          continue;
        }
        List<char> blank = new List<char>();
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

          Console.Out.WriteLine("{0:X8}", APM.keyToIndex(key));

          string path = string.Format("{0}{1}Reward{1}Image{1}ACHIEVEMENT{1}", output, Path.DirectorySeparatorChar);
          if(achievement.Data.image != 0) {
            ExtractImage(achievement.Data.image, path, map, handler, $"{APM.keyToIndex(key):X8}" + "_img");
          }
          if(achievement.Data.icon != 0) {
            ExtractImage(achievement.Data.icon, path, map, handler, $"{APM.keyToIndex(key):X8}" + "_icon");
          }
          if(achievement.Data.reward == 0) {
            continue;
          }
          using(Stream inputReward = Util.OpenFile(map[achievement.Data.reward], handler)) {
            if(inputReward == null) {
              continue;
            }
            STUD reward = new STUD(inputReward);
            if(reward.Instances == null || reward.Instances[0] == null) {
              continue;
            }

            IInventorySTUDInstance instance = reward.Instances[0] as IInventorySTUDInstance;
            if(instance == null) {
              continue;
            }


            switch(reward.Instances[0].Name) {
              case "Icon":
                ExtractLogic.Icon.Extract(reward, output, "Reward", $"{APM.keyToIndex(key):X8}", "ACHIEVEMENT", track, map, handler, blank);
                break;
              case "Spray":
                ExtractLogic.Spray.Extract(reward, output, "Reward", $"{APM.keyToIndex(key):X8}", "ACHIEVEMENT", track, map, handler, blank);
                break;
              default:
                Console.Out.WriteLine("No way to extract {0}!", reward.Instances[0].Name);
                continue;
            }
          }
        }
      }
    }
  }
}
