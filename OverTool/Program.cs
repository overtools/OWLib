using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CASCExplorer;
using OWLib;

namespace OverTool {
  public struct Record {
    public APMPackage package;
    public PackageIndex index;
    public PackageIndexRecord record;
  }

  class Program {
    static void Main(string[] args) {
      if(args.Length < 2) {
        Console.Out.WriteLine("Usage: OverTool.exe \"overwatch path\" mode [opts]");
        Console.Out.WriteLine("THIS TOOL IS UNFINISHED.");
        Console.Out.WriteLine("Modes:");
        Console.Out.WriteLine("\tt - List Cosmetics");
        return;
      }

      string root = args[0];
      char opt = args[1][0];
      char[] validOpts = new char[] { 't', 'x' };
      if(!validOpts.Contains(opt)) {
        Console.Error.WriteLine("UNSUPPORTED OPT {0}", opt);
        return;
      }

      Dictionary<ushort, List<ulong>> track = new Dictionary<ushort, List<ulong>>();
      track.Add(0x75, new List<ulong>());

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

      Console.Out.WriteLine("Mapping...");
      foreach(APMFile apm in ow.APMFiles) {
        if(!apm.Name.ToLowerInvariant().Contains("rdev")) {
          continue; // skip
        }
        for(int i = 0; i < apm.Packages.Length; ++i) {
          APMPackage package = apm.Packages[i];
          PackageIndex index = apm.Indexes[i];
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

            ushort id = (ushort)APM.keyToTypeID(record.Key);
            if(track.ContainsKey(id)) {
              track[id].Add(record.Key);
            }
          }
        }
      }

      if(opt == 't') {
        ListInventory.Parse(track, map, handler, args.Skip(2).ToArray());
      } else if(opt == 'x') {
        Extract.Parse(track, map, handler, args.Skip(2).ToArray());
      }
    }
  }
}
