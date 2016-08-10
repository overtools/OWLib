using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CASCExplorer;
using System.Reflection;
using System.Linq;

namespace PackageTool {
  class Program {
    private static void CopyBytes(Stream i, Stream o, int sz) {
      byte[] buffer = new byte[sz];
      i.Read(buffer, 0, sz);
      o.Write(buffer, 0, sz);
      buffer = null;
    }

    static void Main(string[] args) {
      if(args.Length < 3) {
        Console.Out.WriteLine("Usage: PackageTool.exe [-LLang] \"overwatch_folder\" \"output_folder\" <keys...>");
        Console.Out.WriteLine("Keys must start with 'i' for content keys, 'p' for package keys, 'n' for package indexes, 't' for package indexes + indices");
        Console.Out.WriteLine("Keys must start with 'a' for specific APMs");
        Console.Out.WriteLine("If any key starts with Q it will dump all files.");
        Console.Out.WriteLine("If any key starts with D it will only output filenames to console, but not write files");
        return;
      }

      if(args[0][0] == '-' && args[0][1] == 'L') {
        string lang = args[0].Substring(2);
        OwRootHandler.LanguageScan = lang;
        args = args.Skip(1).ToArray();
      }

      string root = args[0];
      string output = args[1] + Path.DirectorySeparatorChar;

      Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString());

      HashSet<ulong> packageKeys = new HashSet<ulong>();
      HashSet<ulong> packageIndices = new HashSet<ulong>();
      HashSet<ulong> packageIndent = new HashSet<ulong>();
      HashSet<string> contentKeys = new HashSet<string>();
      HashSet<ulong> dumped = new HashSet<ulong>();
      string apmName = null;
      bool dumpAll = false;
      bool dry = false;

      for(int i = 2; i < args.Length; ++i) {
        string arg = args[i];
        switch(arg[0]) {
          case 'q':
          case 'Q':
            dumpAll = true;
            break;
          case 'd':
          case 'D':
            dry = true;
            break;
          case 'p':
            packageKeys.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
            break;
          case 'P':
            packageKeys.Add(ulong.Parse(arg.Substring(1), NumberStyles.Number));
            break;
          case 'n':
            packageIndices.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
            break;
          case 'N':
            packageIndices.Add(ulong.Parse(arg.Substring(1), NumberStyles.Number));
            break;
          case 't':
            packageIndent.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
            break;
          case 'T':
            packageIndent.Add(ulong.Parse(arg.Substring(1), NumberStyles.Number));
            break;
          case 'a':
          case 'A':
            apmName = arg.Substring(1).ToLowerInvariant();
            break;
          case 'i':
          case 'I':
            contentKeys.Add(arg.Substring(1).ToUpperInvariant());
            break;
        }
      }

      if(contentKeys.Count + packageKeys.Count + packageIndices.Count + packageIndent.Count == 0 && !dumpAll) {
        Console.Error.WriteLine("Must have at least 1 query");
        return;
      }
      
      CASCConfig config = CASCConfig.LoadLocalStorageConfig(root);
      CASCHandler handler = CASCHandler.OpenStorage(config);
      OwRootHandler ow = handler.Root as OwRootHandler;
      if(ow == null) {
        Console.Error.WriteLine("Not a valid Overwatch installation");
        return;
      }

      Console.Out.WriteLine("Extracting...");

      HashSet<ulong> indicesExtracted = new HashSet<ulong>();
      foreach(APMFile apm in ow.APMFiles) {
        if(apmName != null && !Path.GetFileName(apm.Name).ToLowerInvariant().Contains(apmName)) {
          continue;
        }
        Console.Out.WriteLine("Iterating {0}", Path.GetFileName(apm.Name));
        for(long i = 0; i < apm.Packages.LongLength; ++i) {
          if(contentKeys.Count + packageKeys.Count + packageIndices.Count + packageIndent.Count == 0 && !dumpAll) {
            return;
          }

          APMPackage package = apm.Packages[i];

          if(!dumpAll) {
            bool ret = true;
            if(packageKeys.Count > 0 && packageKeys.Contains(package.packageKey)) {
              ret = false;
            }

            if(ret && contentKeys.Count > 0 && contentKeys.Contains(package.indexContentKey.ToHexString().ToUpperInvariant())) {
              ret = false;
            }
            
            if(ret && packageIndices.Count > 0 && packageIndices.Contains(OWLib.APM.keyToIndex(package.packageKey)) && !indicesExtracted.Contains(package.packageKey)) {
              ret = false;
            }

            if(ret && packageIndent.Count > 0 && packageIndent.Contains(OWLib.APM.keyToIndexID(package.packageKey))) {
              ret = false;
            }

            if(ret) {
              continue;
            }
          }

          packageKeys.Remove(package.packageKey);
          indicesExtracted.Add(package.packageKey);
          packageIndent.Remove(OWLib.APM.keyToIndexID(package.packageKey));
          contentKeys.Remove(package.indexContentKey.ToHexString().ToUpperInvariant());

          PackageIndex index = apm.Indexes[i];
          PackageIndexRecord[] records = apm.Records[i];

          string o = null;
          if(dumpAll) {
            o = output;
          } else {
            o = string.Format("{0}{1:X12}{2}", output, OWLib.APM.keyToIndexID(package.packageKey), Path.DirectorySeparatorChar);
          }

          EncodingEntry bundleEncoding;
          bool allowBundle = handler.Encoding.GetEntry(index.bundleContentKey, out bundleEncoding);

          Stream bundleStream = null;
          if(allowBundle) {
            try {
              bundleStream = handler.OpenFile(bundleEncoding.Key);
            } catch {
              Console.Error.WriteLine("Cannot open bundle {0:X16} -- encryption", index.bundleKey);
              continue;
            }
          }
          foreach(PackageIndexRecord record in records) {
            if(dumpAll && !dumped.Add(record.Key)) {
              continue;
            }
            ulong rtype = OWLib.APM.keyToTypeID(record.Key);
            ulong rindex = OWLib.APM.keyToIndexID(record.Key);
            string ofn = string.Format("{0}{1:X3}{2}", o, rtype, Path.DirectorySeparatorChar);
            if(!dry && !Directory.Exists(ofn)) {
              Console.Out.WriteLine("Created directory {0}", ofn);
              Directory.CreateDirectory(ofn);
            }
            ofn = string.Format("{0}{1:X12}.{2:X3}", ofn, rindex, rtype);
            if(!dry) {
              using(Stream outputStream = File.Open(ofn, FileMode.Create, FileAccess.Write)) {
                if(((ContentFlags)record.Flags & ContentFlags.Bundle) == ContentFlags.Bundle) {
                  if(allowBundle) {
                    bundleStream.Position = record.Offset;
                    CopyBytes(bundleStream, outputStream, record.Size);
                  } else {
                    Console.Error.WriteLine("Cannot open file {0} -- can't open bundle", ofn);
                    continue;
                  }
                } else {
                  EncodingEntry recordEncoding;
                  if(!handler.Encoding.GetEntry(record.ContentKey, out recordEncoding)) {
                    Console.Error.WriteLine("Cannot open file {0} -- doesn't have bundle flags", ofn);
                    continue;
                  }

                  try {
                    using(Stream recordStream = handler.OpenFile(recordEncoding.Key)) {
                      CopyBytes(recordStream, outputStream, record.Size);
                    }
                  } catch {
                    Console.Error.WriteLine("Cannot open file {0} -- encryption", ofn);
                  }
                }
              }
            }

            Console.Out.WriteLine("Saved file {0}", ofn);
          }

          if(allowBundle) {
            bundleStream.Dispose();
          }
        }
      }
    }
  }
}
