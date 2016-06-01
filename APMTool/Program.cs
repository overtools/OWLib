using System;
using System.Collections.Generic;
using System.Globalization;
using CASCExplorer;
using System.Reflection;

namespace APMTool {
  class Program {
    static void Main(string[] args) {
      // APMTool.exe "root" file flag [query]
      if(args.Length < 2) {
        Console.Out.WriteLine("Usage: APMTool.exe \"root directory\" op args");
        Console.Out.WriteLine("OP f: Find files in APM. subop: a (match all) args: query... query: i[INDEX HEX] t[TYPE HEX] I[INDEX] T[TYPE]");
        Console.Out.WriteLine("OP l: List files in package. args: query query: p[PACKAGE KEY HEX] i[CONTENT KEY HEX]");
        Console.Out.WriteLine("");
        Console.Out.WriteLine("Examples:");
        Console.Out.WriteLine("APMTool.exe overwatch l iDFEF49BEE7E66774E46DA9EEA750A552");
        Console.Out.WriteLine("APMTool.exe overwatch fa i11CE t00C");
        Console.Out.WriteLine("APMTool.exe overwatch fa i4F2");
        return;
      }
      string root = args[0];
      string flag = args[1];

      Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString());

      object[] query = null;
      bool glob = false;
      if(flag[0] == 'f') {
        glob = flag.Length > 1 && flag[1] == 'a';
        object[] t = new object[2] { null, null };

        List<ulong> id = new List<ulong>();
        List<ulong> type = new List<ulong>();
        for(int i = 2; i < args.Length; ++i) {
          string arg = args[i];
          try {
            switch(arg[0]) {
              case 'i':
                id.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
                break;
              case 't':
                type.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
                break;
              case 'I':
                id.Add(ulong.Parse(arg.Substring(1), NumberStyles.Number));
                break;
              case 'T':
                type.Add(ulong.Parse(arg.Substring(1), NumberStyles.Number));
                break;
            }
          } finally {
            t[0] = id;
            t[1] = type;
            query = t;
          }
        }
      } else if(flag[0] == 'l') {
        object[] t = new object[2] { null, null };
        List<ulong> pkg = new List<ulong>();
        List<string> content = new List<string>();
        for(int i = 2; i < args.Length; ++i) {
          string arg = args[i];
          try {
            switch(arg[0]) {
              case 'p':
                pkg.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
                break;
              case 'P':
                pkg.Add(ulong.Parse(arg.Substring(1), NumberStyles.Number));
                break;
              case 'i':
                content.Add(arg.Substring(1).ToUpperInvariant());
                break;
            }
          } finally {
            t[0] = pkg;
            t[1] = content;
            query = t;
          }
        }
      } else {
        Console.Error.WriteLine("Unsupported operand {0}", flag[0]);
        return;
      }
      
      CASCConfig config = CASCConfig.LoadLocalStorageConfig(root);
      CASCHandler handler = CASCHandler.OpenStorage(config);
      OwRootHandler ow = handler.Root as OwRootHandler;
      if(ow == null) {
        Console.Error.WriteLine("Not a valid Overwatch installation");
        return;
      }

      foreach(APMFile apm in ow.APMFiles) {
        for(long i = 0; i < apm.Packages.LongLength; ++i) {
          APMPackage package = apm.Packages[i];
          PackageIndexRecord[] records = apm.Records[i];

          if(flag[0] == 'l') {
            if(((List<ulong>)query[0]).Count > 0 && !((List<ulong>)query[0]).Contains(package.packageKey)) {
              continue;
            }

            if(((List<string>)query[1]).Count > 0 && !((List<string>)query[1]).Contains(package.indexContentKey.ToHexString().ToUpperInvariant())) {
              continue;
            }

            Console.Out.WriteLine("Dump for package i{0} / p{1:X}", package.indexContentKey.ToHexString().ToUpperInvariant(), package.packageKey);
          }

          for(long j = 0; j < records.LongLength; ++j) {
            PackageIndexRecord record = records[j];

            ulong rtype = OWLib.APM.keyToTypeID(record.Key);
            ulong rindex = OWLib.APM.keyToIndexID(record.Key);

            if(flag[0] == 'f') {
              bool check1 = true;
              bool check2 = true;
              if(((List<ulong>)query[0]).Count > 0 && !((List<ulong>)query[0]).Contains(rindex)) {
                check1 = false;
              }
              if(((List<ulong>)query[1]).Count > 0 && !((List<ulong>)query[1]).Contains(rtype)) {
                check2 = false;
              }
              bool check = glob ? (check1 && check2) : (check1 || check2);
              if(check) {
                Console.Out.WriteLine("Found {0:X16}.{1:X4} in package i{2} / p{3:X}", rindex, rtype, package.indexContentKey.ToHexString().ToUpperInvariant(), package.packageKey);
              }
            } else if (flag[0] == 'l') {
              Console.Out.WriteLine("\t{0:X16}.{1:X4} ({2} bytes) - {3:X}", rindex, rtype, record.Size, record.Key);
            }
          }
        }
      }
    }
  }
}
