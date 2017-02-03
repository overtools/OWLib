using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;

namespace OverTool {
  public struct Record {
    public APMPackage package;
    public PackageIndex index;
    public PackageIndexRecord record;
  }

  class Program {
    static string[] ValidLangs = new string[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW" };

    static void Main(string[] args) {
      Console.OutputEncoding = Encoding.UTF8;
      List<IOvertool> tools = new List<IOvertool>();

      {
        Assembly asm = typeof(IOvertool).Assembly;
        Type t = typeof(IOvertool);
        List<Type> types = asm.GetTypes().Where(tt => tt != t && t.IsAssignableFrom(tt)).ToList();
        foreach(Type tt in types) {
          if(tt.IsInterface) {
            continue;
          }
          tools.Add((IOvertool)Activator.CreateInstance(tt));
        }
      }

      if(args.Length < 2) {
        PrintHelp(tools);
        return;
      }

      if(args[0][0] == '-' && args[0][1] == 'L') {
        string lang = args[0].Substring(2);
        if(!ValidLangs.Contains(lang)) {
          Console.Out.WriteLine("Language {0} is not supported!", lang);
          foreach(string validLang in ValidLangs) {
            if(validLang.ToLowerInvariant().Contains(lang.ToLowerInvariant())) {
              lang = validLang;
              Console.Out.WriteLine("Autocorrecting selected lanuage to {0}", lang);
              break;
            }
          }
        }
        if(!ValidLangs.Contains(lang)) {
          return;
        }
        Console.Out.WriteLine("Set language to {0}", lang);
        OwRootHandler.LanguageScan = lang;
        args = args.Skip(1).ToArray();
      }

      bool enableKeyDetection = true;
      if(args[0][0] == '-' && args[0][1] == 'n') {
        enableKeyDetection = false;
        Console.Out.WriteLine("Disabling Key auto-detection...");
        args = args.Skip(1).ToArray();
      }

      string root = args[0];
      char opt = args[1][0];
      
      if(args.Length < 2) {
        PrintHelp(tools);
        return;
      }
      
      IOvertool tool = null;
      Dictionary<ushort, List<ulong>> track = new Dictionary<ushort, List<ulong>>();

      foreach(IOvertool t in tools) {
        if(t.Opt == opt) {
          tool = t;
        }

        foreach(ushort tr in t.Track) {
          if(!track.ContainsKey(tr)) {
            track[tr] = new List<ulong>();
          }
        }
      }
      if(tool == null || args.Length - 2 < tool.MinimumArgs) {
        PrintHelp(tools);
        return;
      }

      Dictionary<ulong, Record> map = new Dictionary<ulong, Record>();

      Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString());
      Console.Out.WriteLine("Initializing CASC...");
      CASCConfig config = CASCConfig.LoadLocalStorageConfig(root);
      CASCHandler handler = CASCHandler.OpenStorage(config);
      OwRootHandler ow = handler.Root as OwRootHandler;
      if(ow == null) {
        Console.Error.WriteLine("Not a valid overwatch installation");
        return;
      }

      // Fail when trying to extract data from a specified language with 2 or less files found.
      if(ow.APMFiles.Count() == 0) {
        Console.Error.WriteLine("Could not find the files for language {0}. Please confirm that you have that language installed, and are using the names from the target language.", OwRootHandler.LanguageScan);
        return;
      }

      Console.Out.WriteLine("Mapping...");
      foreach(APMFile apm in ow.APMFiles) {
        if(!apm.Name.ToLowerInvariant().Contains("rdev")) {
          continue; // skip
        }
        //Console.Out.WriteLine("Package Length: {0}", apm.Packages.Length);
        for(int i = 0; i < apm.Packages.Length; ++i) {
          //Console.Out.WriteLine("i: {0}",i);
          APMPackage package = apm.Packages[i];
          //Console.Out.WriteLine("Package: {0}", package);
          PackageIndex index = apm.Indexes[i];
          //Console.Out.WriteLine("Package Index: {0}", index);
          PackageIndexRecord[] records = apm.Records[i];
          for(long j = 0; j < records.LongLength; ++j) {
            PackageIndexRecord record = records[j];
            if(map.ContainsKey(record.Key)) {
              continue;
            }

            Record rec = new Record {
              package = package,
              index = index,
              record = record,
            };
            map.Add(record.Key, rec);
          }
        }
      }
      int origLength = map.Count;
      foreach(APMFile apm in ow.APMFiles) {
        if(!apm.Name.ToLowerInvariant().Contains("rdev")) {
          continue;
        }
        foreach(KeyValuePair<ulong, CMFHashData> pair in apm.CMFMap) {
          ushort id = (ushort)APM.keyToTypeID(pair.Value.id);
          if(track.ContainsKey(id)) {
            track[id].Add(pair.Value.id);
          }

          if(map.ContainsKey(pair.Key)) {
            continue;
          }
          Record rec = new Record {
            record = new PackageIndexRecord()
          };
          rec.record.Flags = 0;
          rec.record.Key = pair.Value.id;
          rec.record.ContentKey = pair.Value.HashKey;
          rec.package = new APMPackage(new APMPackageItem());
          rec.index = new PackageIndex(new PackageIndexItem());

          EncodingEntry enc;
          if(handler.Encoding.GetEntry(pair.Value.HashKey, out enc)) {
            rec.record.Size = enc.Size;
            map.Add(pair.Key, rec);
          }
        }
      }
      if(origLength != map.Count) {
        Console.Out.WriteLine("Warning: {0} packageless files", map.Count - origLength);
      }

      if(enableKeyDetection) {
        Console.Out.WriteLine("Adding Encryption Keys...");

        foreach(ulong key in track[0x90]) {
          if(!map.ContainsKey(key)) {
            continue;
          }
          using(Stream stream = Util.OpenFile(map[key], handler)) {
            if(stream == null) {
              continue;
            }
            STUD stud = new STUD(stream);
            if(stud.Instances[0].Name != stud.Manager.GetName(typeof(EncryptionKey))) {
              continue;
            }
            EncryptionKey ek = (EncryptionKey)stud.Instances[0];
            if(!KeyService.keys.ContainsKey(ek.KeyNameLong)) {
              KeyService.keys.Add(ek.KeyNameLong, ek.KeyValueText.ToByteArray());
              Console.Out.WriteLine("Added Encryption Key {0}", ek.KeyNameText);
            }
          }
        }
      }

      Console.Out.WriteLine("Tooling...");

      tool.Parse(track, map, handler, args.Skip(2).ToArray());
      if(System.Diagnostics.Debugger.IsAttached) {
        System.Diagnostics.Debugger.Break();
      }
    }

    internal class OvertoolComparer : IComparer<IOvertool> {
      public int Compare(IOvertool x, IOvertool y) {
        return string.Compare(x.Title, y.Title);
      }
    }

    private static void PrintHelp(List<IOvertool> tools) {
      Console.Out.WriteLine("Usage: OverTool.exe [-LLang] \"overwatch path\" mode [mode opts]");
      Console.Out.WriteLine("Options:");
      Console.Out.WriteLine("\tL - Specify a language to extract. Example: -LdeDE");
      Console.Out.WriteLine("\t\tValid Languages: {0}", string.Join(", ", ValidLangs));
      Console.Out.WriteLine("mode can be:");
      Console.Out.WriteLine("  m - {0, -30} - {1, -30}", "name", "arguments");
      Console.Out.WriteLine("".PadLeft(64, '-'));
      tools.Sort(new OvertoolComparer());
      foreach(IOvertool t in tools) {
        Console.Out.WriteLine("  {0} - {1,-30} - {2}", t.Opt, t.Title, t.Help);
      }
      return;
    }
  }
}