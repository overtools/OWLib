using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CASCExplorer;
using System.Reflection;
using System.Linq;
using OWLib;

namespace PackageTool {
    class Program {
        private static void CopyBytes(Stream i, Stream o, int sz) {
            byte[] buffer = new byte[sz];
            i.Read(buffer, 0, sz);
            o.Write(buffer, 0, sz);
            buffer = null;
        }

        static void Main(string[] args) {
            if (args.Length < 3) {
                Console.Out.WriteLine("Usage: PackageTool.exe [-LLang] \"overwatch_folder\" \"output_folder\" <keys...>");
                Console.Out.WriteLine("Keys must start with 'i' for content keys, 'p' for package keys, 'n' for package indexes, 't' for package indexes + indices");
                Console.Out.WriteLine("Keys must start with 'a' for specific APMs");
                Console.Out.WriteLine("If any key starts with Q it will dump all files.");
                Console.Out.WriteLine("If any key starts with D it will only output filenames to console, but not write files");
                return;
            }

            string root = args[0];
            
            OwRootHandler.LOAD_PACKAGES = true;
            CASCConfig config = null;
            // ngdp:us:pro
            // http:us:pro:us.patch.battle.net:1119
            if (root.ToLowerInvariant().Substring(0, 5) == "ngdp:") {
                string cdn = root.Substring(5, 4);
                string[] parts = root.Substring(5).Split(':');
                string region = "us";
                string product = "pro"; 
                if (parts.Length > 1) {
                    region = parts[1];
                }
                if (parts.Length > 2) {
                    product = parts[2];
                }
                if (cdn == "bnet") {
                    config = CASCConfig.LoadOnlineStorageConfig(product, region);
                } else {
                    if (cdn == "http") {
                        string host = string.Join(":", parts.Skip(3));
                        config = CASCConfig.LoadOnlineStorageConfig(host, product, region, true, true);
                    }
                }
            } else {
                config = CASCConfig.LoadLocalStorageConfig(root, true, true);
            }
            if (args[0][0] == '-' && args[0][1] == 'L') {
                string lang = args[0].Substring(2);
                config.Languages = new HashSet<string>(new string[1] { lang });
                args = args.Skip(1).ToArray();
            }

            string output = args[1] + Path.DirectorySeparatorChar;
            
            Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, OWLib.Util.GetVersion());

            HashSet<ulong> packageKeys = new HashSet<ulong>();
            HashSet<ulong> packageIndices = new HashSet<ulong>();
            HashSet<ulong> packageIndent = new HashSet<ulong>();
            HashSet<string> contentKeys = new HashSet<string>();
            HashSet<ulong> dumped = new HashSet<ulong>();
            HashSet<ulong> fileKeys = new HashSet<ulong>();
            HashSet<ulong> types = new HashSet<ulong>();
            string apmName = null;
            bool dumpAll = false;
            bool dry = false;

            for (int i = 2; i < args.Length; ++i) {
                string arg = args[i];
                switch (arg[0]) {
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
                    case 'f':
                        fileKeys.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
                        break;
                    case 'F':
                        fileKeys.Add(ulong.Parse(arg.Substring(1), NumberStyles.Number));
                        break;
                    case 'M':
                        types.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
                        break;
                }
            }

            if (contentKeys.Count + packageKeys.Count + packageIndices.Count + packageIndent.Count + fileKeys.Count + types.Count == 0 && !dumpAll) {
                Console.Error.WriteLine("Must have at least 1 query");
                return;
            }

            CASCHandler handler = CASCHandler.OpenStorage(config);
            OwRootHandler ow = handler.Root as OwRootHandler;
            if (ow == null) {
                Console.Error.WriteLine("Not a valid Overwatch installation");
                return;
            }

            Console.Out.WriteLine("Extracting...");

            HashSet<ulong> indicesExtracted = new HashSet<ulong>();
            HashSet<ulong> CMFExtracted = new HashSet<ulong>();
            foreach (APMFile apm in ow.APMFiles) {
                if (apmName != null && !Path.GetFileName(apm.Name).ToLowerInvariant().Contains(apmName)) {
                    continue;
                }
                Console.Out.WriteLine("Iterating {0}", Path.GetFileName(apm.Name));
                HashSet<ulong> removed = new HashSet<ulong>();
                foreach (ulong key in fileKeys) {
                    if (apm.CMFMap.ContainsKey(key)) {
                        ulong rtype = GUID.Type(key);
                        ulong rindex = GUID.LongKey(key);
                        string ofn = $"{output}{Path.DirectorySeparatorChar}cmf{Path.DirectorySeparatorChar}{rtype:X3}{Path.DirectorySeparatorChar}";
                        if (!dry && !Directory.Exists(ofn)) {
                            Console.Out.WriteLine("Created directory {0}", ofn);
                            Directory.CreateDirectory(ofn);
                        }
                        ofn = $"{ofn}{rindex:X12}.{rtype:X3}";
                        if (!dry) {
                            using (Stream outputStream = File.Open(ofn, FileMode.Create, FileAccess.Write)) {
                                EncodingEntry recordEncoding;
                                if (!handler.Encoding.GetEntry(apm.CMFMap[key].HashKey, out recordEncoding)) {
                                    Console.Error.WriteLine("Cannot open file {0} -- malformed CMF?", ofn);
                                    continue;
                                }

                                try {
                                    using (Stream recordStream = handler.OpenFile(recordEncoding.Key)) {
                                        CopyBytes(recordStream, outputStream, recordEncoding.Size);
                                    }
                                    Console.Out.WriteLine("Saved file {0}", ofn);
                                    removed.Add(key);
                                } catch {
                                    Console.Error.WriteLine("Cannot open file {0} -- encryption", ofn);
                                }
                            }
                        }
                    }
                }
                foreach (ulong key in removed) {
                    fileKeys.Remove(key);
                }
                if (types.Count > 0 || dumpAll) {
                    foreach (ulong key in apm.CMFMap.Keys) {
                        if (types.Contains(GUID.Type(key)) || dumpAll) {
                            ulong rtype = GUID.Type(key);
                            ulong rindex = GUID.LongKey(key);
                            string ofn = $"{output}{Path.DirectorySeparatorChar}cmf{Path.DirectorySeparatorChar}{rtype:X3}{Path.DirectorySeparatorChar}";
                            if (!dry && !Directory.Exists(ofn)) {
                                Console.Out.WriteLine("Created directory {0}", ofn);
                                Directory.CreateDirectory(ofn);
                            }
                            ofn = $"{ofn}{rindex:X12}.{rtype:X3}";
                            if (!dry) {
                                using (Stream outputStream = File.Open(ofn, FileMode.Create, FileAccess.Write)) {
                                    EncodingEntry recordEncoding;
                                    if (!handler.Encoding.GetEntry(apm.CMFMap[key].HashKey, out recordEncoding)) {
                                        Console.Error.WriteLine("Cannot open file {0} -- malformed CMF?", ofn);
                                        continue;
                                    }

                                    try {
                                        using (Stream recordStream = handler.OpenFile(recordEncoding.Key)) {
                                            CopyBytes(recordStream, outputStream, recordEncoding.Size);
                                        }
                                        Console.Out.WriteLine("Saved file {0}", ofn);
                                    } catch {
                                        Console.Error.WriteLine("Cannot open file {0} -- encryption", ofn);
                                    }
                                }
                            }
                        }
                    }
                }
                if (dumpAll) {
                    continue;
                }
                if (contentKeys.Count + packageKeys.Count + packageIndices.Count + packageIndent.Count + fileKeys.Count > 0) {
                    for (long i = 0; i < apm.Packages.LongLength; ++i) {
                        if (contentKeys.Count + packageKeys.Count + packageIndices.Count + packageIndent.Count + fileKeys.Count == 0) {
                            break;
                        }

                        APMPackage package = apm.Packages[i];

                        if (!dumpAll) {
                            bool ret = true;
                            if (packageKeys.Count > 0 && packageKeys.Contains(package.packageKey)) {
                                ret = false;
                            }

                            if (ret && contentKeys.Count > 0 && contentKeys.Contains(package.indexContentKey.ToHexString().ToUpperInvariant())) {
                                ret = false;
                            }

                            if (ret && packageIndices.Count > 0 && packageIndices.Contains(GUID.Index(package.packageKey)) && !indicesExtracted.Contains(package.packageKey)) {
                                ret = false;
                            }

                            if (ret && packageIndent.Count > 0 && packageIndent.Contains(GUID.LongKey(package.packageKey))) {
                                ret = false;
                            }

                            if (ret) {
                                continue;
                            }
                        }

                        packageKeys.Remove(package.packageKey);
                        indicesExtracted.Add(package.packageKey);
                        packageIndent.Remove(GUID.LongKey(package.packageKey));
                        contentKeys.Remove(package.indexContentKey.ToHexString().ToUpperInvariant());

                        PackageIndex index = apm.Indexes[i];
                        PackageIndexRecord[] records = apm.Records[i];

                        string o = null;
                        if (dumpAll) {
                            o = output;
                        } else {
                            o = $"{output}{GUID.LongKey(package.packageKey):X12}{Path.DirectorySeparatorChar}";
                        }

                        EncodingEntry bundleEncoding;
                        bool allowBundle = handler.Encoding.GetEntry(index.bundleContentKey, out bundleEncoding);

                        Stream bundleStream = null;
                        if (allowBundle) {
                            try {
                                bundleStream = handler.OpenFile(bundleEncoding.Key);
                            } catch {
                                Console.Error.WriteLine("Cannot open bundle {0:X16} -- encryption", index.bundleKey);
                                continue;
                            }
                        }
                        foreach (PackageIndexRecord record in records) {
                            if (dumpAll && !dumped.Add(record.Key)) {
                                continue;
                            }
                            ulong rtype = GUID.Type(record.Key);
                            ulong rindex = GUID.LongKey(record.Key);
                            string ofn = $"{o}{rtype:X3}{Path.DirectorySeparatorChar}";
                            if (!dry && !Directory.Exists(ofn)) {
                                Console.Out.WriteLine("Created directory {0}", ofn);
                                Directory.CreateDirectory(ofn);
                            }
                            ofn = $"{ofn}{rindex:X12}.{rtype:X3}";
                            if (!dry) {
                                using (Stream outputStream = File.Open(ofn, FileMode.Create, FileAccess.Write)) {
                                    if (((ContentFlags)record.Flags & ContentFlags.Bundle) == ContentFlags.Bundle) {
                                        if (allowBundle) {
                                            bundleStream.Position = record.Offset;
                                            CopyBytes(bundleStream, outputStream, record.Size);
                                        } else {
                                            Console.Error.WriteLine("Cannot open file {0} -- can't open bundle", ofn);
                                            continue;
                                        }
                                    } else {
                                        EncodingEntry recordEncoding;
                                        if (!handler.Encoding.GetEntry(record.ContentKey, out recordEncoding)) {
                                            Console.Error.WriteLine("Cannot open file {0} -- doesn't have bundle flags", ofn);
                                            continue;
                                        }

                                        try {
                                            using (Stream recordStream = handler.OpenFile(recordEncoding.Key)) {
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

                        if (allowBundle) {
                            bundleStream.Dispose();
                        }
                    }
                }
            }
        }
    }
}
