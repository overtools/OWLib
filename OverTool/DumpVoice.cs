using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool {
  class DumpVoice {
    public static void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
      if(args.Length < 1) {
        Console.Out.WriteLine("Usage: OverTool.exe overwatch v output [hero query]");
        return;
      }

      string output = args[0];

      List<string> heroes = new List<string>();
      if(args.Length > 1) {
        heroes.AddRange(args[1].ToLowerInvariant().Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries));
      }
      bool heroAllWildcard = heroes.Count == 0 || heroes.Contains("*");

      List<ulong> masters = track[0x75];
      foreach(ulong masterKey in masters) {
        if(!map.ContainsKey(masterKey)) {
          continue;
        }
        STUD masterStud = new STUD(Util.OpenFile(map[masterKey], handler));
        if(masterStud.Instances == null || masterStud.Instances[0] == null) {
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
        if(!heroes.Contains(heroName.ToLowerInvariant())) {
          if(!heroAllWildcard) {
            continue;
          }
        }
        Console.Out.WriteLine("Dumping voice bites for hero {0}", heroName);
        Dictionary<ulong, List<ExtractLogic.VoiceLine.SoundOwnerPair>> soundData = ExtractLogic.VoiceLine.FindSounds(master, track, map, handler);
        string path = string.Format("{0}{1}{2}{1}{3}{1}", output, Path.DirectorySeparatorChar, Util.SanitizePath(heroName), "Sound Dump");
        if(!Directory.Exists(path)) {
          Directory.CreateDirectory(path);
        }
        HashSet<ulong> done = new HashSet<ulong>();
        foreach(List<ExtractLogic.VoiceLine.SoundOwnerPair> list in soundData.Values) {
          uint sigma = 0;
          List<ulong> sounds = ExtractLogic.VoiceLine.FlattenSounds(list, map, handler);
          foreach(ulong key in sounds) {
            if(!done.Add(key)) {
              continue;
            }
            string outputPath = string.Format("{0}{1:X12}", path, APM.keyToIndexID(key));
            if(sigma > 0) {
              outputPath += string.Format("_{0}", sigma);
            }
            sigma += 1;
            outputPath += ".wem";
            using(Stream soundStream = Util.OpenFile(map[key], handler)) {
              using(Stream outputStream = File.Open(outputPath, FileMode.Create)) {
                ExtractLogic.VoiceLine.CopyBytes(soundStream, outputStream, (int)soundStream.Length);
                Console.Out.WriteLine("Wrote file {0}", outputPath);
              }
            }
          }
        }
      }
    }
  }
}
