using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using OWLib;
using OWLib.Types;

namespace APMTool {
  class Program {
    static void Main(string[] args) {
      // APMTool.exe "root" file flag [query]
      if(args.Length < 3 || (args[2][0] != 'f' && args[2][0] != 'l' && args[2][0] != 'c')) {
        Console.Out.WriteLine(string.Format("Usage: APMTool.exe \"root directory\" file op args"));
        Console.Out.WriteLine("OP f: Find files in APM. subop: a (match all) args: query... query: i[INDEX HEX] t[TYPE HEX] I[INDEX] T[TYPE]");
        Console.Out.WriteLine("OP l: List files in package. args: query query: p[PACKAGE KEY HEX] i[CONTENT KEY HEX]");
        Console.Out.WriteLine("OP c: Comare files in APM. args: \"old root directory\" old_file");
        Console.Out.WriteLine("");
        Console.Out.WriteLine("Examples:");
        Console.Out.WriteLine("APMTool.exe apmroot Win_SPWin_RCN_LenUS_EExt d iDFEF49BEE7E66774E46DA9EEA750A552");
        Console.Out.WriteLine("APMTool.exe apmroot Win_SPWin_RCN_LenUS_EExt fa i11CE t00C");
        Console.Out.WriteLine("APMTool.exe apmroot Win_SPWin_RCN_LenUS_EExt fa i4F2");
        return;
      }
      string root = args[0];
      string name = args[1];
      string flag = args[2];

      Console.Out.WriteLine("APMTool v0.0.1");

      Console.Out.WriteLine("Opening APM structure {0}", name);

      LookupContentByKeyDelegate lookupContentByKey = (delegate(ulong key) {
        return File.Open(string.Format("{0}/{1}/package_{2:X16}.index", root, name, key), FileMode.Open, FileAccess.Read);
      });

      using(Stream apmStream = File.Open(string.Format("{0}/{1}.apm", root, name), FileMode.Open, FileAccess.Read)) {
        APM apm = new APM(apmStream, lookupContentByKey);
        Console.Out.WriteLine("Opened.");
        if(flag[0] == 'l') { // list
          object search = null;

          string arg = args[3];

          bool packageKey = true;
          if(arg[0] == 'p') {
            search = ulong.Parse(arg.Substring(1), NumberStyles.HexNumber);
          } else if(arg[0] == 'i') {
            packageKey = false;
            search = arg.Substring(1);
          } else {
            return;
          }

          for(int i = 0; i < apm.Packages.Length; ++i) {
            APMPackage package = apm.Packages[i];
            if((packageKey && package.packageKey != (ulong)search) || (!packageKey && package.indexContentKey.ToHex() != (string)search)) {
              continue;
            }
            PackageIndexRecord[] records = apm.Records[i];
            Console.Out.WriteLine(string.Format("Dump for package {0:X16}", arg.Substring(1)));
            for(int j = 0; j < records.Length; ++j) {
              PackageIndexRecord record = records[j];
              ulong type = APM.keyToTypeID(record.Key);
              ulong index = APM.keyToIndexID(record.Key);
              Console.Out.WriteLine(string.Format("\t{0:X16}.{1:X4} ({2} bytes)", index, type, record.Size));
            }
            break;
          }
        } else if(flag[0] == 'f') { // find
          Console.Out.WriteLine("Finding...");
          List<ulong> types = new List<ulong>();
          List<ulong> ids = new List<ulong>();
          for(int i = 3; i < args.Length; ++i) {
            string arg = args[i];
            if(arg[0] == 't') {
              types.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
              Console.Out.WriteLine("Added Type {0}", types[types.Count - 1]);
            }
            if(arg[0] == 'i') {
              ids.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
              Console.Out.WriteLine("Added ID {0}", ids[ids.Count - 1]);
            }
            if(arg[0] == 'T') {
              types.Add(ulong.Parse(arg.Substring(1), NumberStyles.Number));
              Console.Out.WriteLine("Added Type {0}", types[types.Count - 1]);
            }
            if(arg[0] == 'I') {
              ids.Add(ulong.Parse(arg.Substring(1), NumberStyles.Number));
              Console.Out.WriteLine("Added ID {0}", ids[ids.Count - 1]);
            }
          }

          bool matchAll = flag.Length > 1 && flag[1] == 'a';
          for(int i = 0; i < apm.Packages.Length; ++i) {
            APMPackage package = apm.Packages[i];
            PackageIndexRecord[] records = apm.Records[i];

            for(int j = 0; j < records.Length; ++j) {
              PackageIndexRecord record = records[j];
              ulong type = APM.keyToTypeID(record.Key);
              ulong index = APM.keyToIndexID(record.Key);

              bool check1 = true;
              bool check2 = true;
              if(types.Count > 0 && !types.Contains(type)) {
                check1 = false;
              }

              if(ids.Count > 0 && !ids.Contains(index)) {
                check2 = false;
              }

              bool check = matchAll ? (check1 && check2) : (check1 || check2);
              if(check) {
                Console.Out.WriteLine(string.Format("Found {0:X16}.{1:X4} in package {2:X}", index, type, package.indexContentKey.ToHex()));
              }
            }
          }
        } else if(flag[0] == 'c') { //compare
          Console.Out.WriteLine("Coming soon");
        } // end flag switch
      }
    }
  }
}
