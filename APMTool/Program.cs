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
        Console.Out.WriteLine("OP f: Find files in APM. subop: a (match all) args: query... query: a[APM NAME] i[INDEX HEX] t[TYPE HEX] I[INDEX] T[TYPE] s[SIZE LESS THAN] S[SIZE GREATER THAN]");
        Console.Out.WriteLine("OP l: List files in package. args: query query: p[PACKAGE KEY HEX] i[CONTENT KEY HEX]");
        Console.Out.WriteLine("OP c: Convert number into index + type. args: hex");
        Console.Out.WriteLine("OP a: Output all APM names");
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
      
      if(flag == "c") {
        ulong value = ulong.Parse(args[2], NumberStyles.HexNumber);
        Console.Out.WriteLine("Value: {0:X}", value);
        Console.Out.WriteLine("Type: {0:X3}", OWLib.APM.keyToTypeID(value));
        Console.Out.WriteLine("Index: {0:X16}", OWLib.APM.keyToIndexID(value));
        return;
      }

      object[] query = null;
      bool glob = false;
      if(flag[0] == 'f') {
        glob = flag.Length > 1 && flag[1] == 'a';
        object[] t = new object[5] { null, null, null, null, null };

        List<ulong> id = new List<ulong>();
        List<ulong> type = new List<ulong>();
        List<string> apms = new List<string>();
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
              case 'a':
                apms.Add(arg.Substring(1).ToLowerInvariant());
                break;
              case 's': {
                  int v = int.Parse(arg.Substring(1), NumberStyles.Number);
                  if(t[2] == null) {
                    t[2] = v;
                  } else if(v > (int)t[2]) {
                    t[2] = v;
                  }
                }
                break;
              case 'S': {
                  int v = int.Parse(arg.Substring(1), NumberStyles.Number);
                  if(t[3] == null) {
                    t[3] = v;
                  } else if(v > (int)t[3]) {
                    t[3] = v;
                  }
                }
                break;
            }
          } finally {
            t[0] = id;
            t[1] = type;
            t[4] = apms;
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
      } else if(flag[0] == 'a') {
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
        if(flag[0] == 'f' && query[4] != null && ((List<string>)query[4]).Count > 0 && !((List<string>)query[4]).Contains(apm.Name.ToLowerInvariant())) {
          continue;
        } else if(flag[0] == 'a') {
          Console.Out.WriteLine(apm.Name);
          continue;
        }

        for(long i = 0; i < apm.Packages.LongLength; ++i) {
          APMPackage package = apm.Packages[i];
          PackageIndexRecord[] records = apm.Records[i];

          if(flag[0] == 'l') {
            if(((List<ulong>)query[0]).Count + ((List<ulong>)query[1]).Count == 0) {
              return;
            }

            bool ret = true;
            if(((List<ulong>)query[0]).Count > 0 && ((List<ulong>)query[0]).Contains(package.packageKey)) {
              ret = false;
            }

            if(ret && ((List<string>)query[1]).Count > 0 && ((List<string>)query[1]).Contains(package.indexContentKey.ToHexString().ToUpperInvariant())) {
              ret = false;
            }

            if(ret) {
              continue;
            }

            ((List<ulong>)query[0]).Remove(package.packageKey);
            ((List<string>)query[1]).Remove(package.indexContentKey.ToHexString().ToUpperInvariant());

            Console.Out.WriteLine("Dump for package i{0} / p{1:X} in APM {2}", package.indexContentKey.ToHexString().ToUpperInvariant(), package.packageKey, apm.Name);
          }

          for(long j = 0; j < records.LongLength; ++j) {
            PackageIndexRecord record = records[j];

            ulong rtype = OWLib.APM.keyToTypeID(record.Key);
            ulong rindex = OWLib.APM.keyToIndexID(record.Key);

            if(flag[0] == 'f') {
              bool check1 = true;
              bool check2 = true;
              bool check3 = true;
              bool check4 = true;
              if(((List<ulong>)query[0]).Count > 0 && !((List<ulong>)query[0]).Contains(rindex)) { // if index is not in i
                check1 = false;
              }
              if(((List<ulong>)query[1]).Count > 0 && !((List<ulong>)query[1]).Contains(rtype)) { // if type is not in t
                check2 = false;
              }
              if(query[2] != null && (int)query[2] < record.Size) { // if size is greater than s[lt]
                check2 = false;
              }
              if(query[3] != null && (int)query[3] > record.Size) { // if size is less than s[gt]
                check3 = false;
              }
              bool check = glob ? (check1 && check2 && check3 && check4) : (check1 || check2 || check3 || check4);
              if(check) {
                Console.Out.WriteLine("Found {0:X16}.{1:X4} in package i{2} / p{3:X} in APM {4}", rindex, rtype, package.indexContentKey.ToHexString().ToUpperInvariant(), package.packageKey, apm.Name);
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
