using System;
using System.Collections.Generic;
using System.Globalization;
using CASCExplorer;
using System.Reflection;
using OWLib;

namespace APMTool {
    class Program {
        static void Main(string[] args) {
            // APMTool.exe "root" file flag [query]
            if (args.Length < 2) {
                Console.Out.WriteLine("Usage: APMTool.exe \"root directory\" op args");
                Console.Out.WriteLine("OP f: Find files in APM. args: query... query: a[APM NAME] i[INDEX HEX] t[TYPE HEX] s[SIZE LESS THAN] S[SIZE GREATER THAN] N[INDEX WITHOUT IDENTIFIER]");
                Console.Out.WriteLine("OP l: List files in package. args: query query: p[PACKAGE KEY HEX] i[CONTENT KEY HEX]");
                Console.Out.WriteLine("OP a: Output all APM names");
                Console.Out.WriteLine("OP t: Output all types");
                Console.Out.WriteLine("");
                Console.Out.WriteLine("Examples:");
                Console.Out.WriteLine("APMTool.exe overwatch l iDFEF49BEE7E66774E46DA9EEA750A552");
                Console.Out.WriteLine("APMTool.exe overwatch fa i11CE t00C");
                Console.Out.WriteLine("APMTool.exe overwatch fa i4F2");
                return;
            }
            string root = args[0];
            string flag = args[1];
            
            Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, OWLib.Util.GetVersion());
            bool all_langs = false;
            object[] query = null;
            if (flag[0] == 'f' || flag[0] == 'C') {
                object[] t = new object[6] { null, null, null, null, null, null };

                List<ulong> id = new List<ulong>();
                List<ulong> type = new List<ulong>();
                List<string> apms = new List<string>();
                List<ulong> idi = new List<ulong>();
                for (int i = 2; i < args.Length; ++i) {
                    string arg = args[i];
                    try {
                        switch (arg[0]) {
                            case 'I':
                            case 'i':
                                id.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
                                break;
                            case 'T':
                            case 't':
                                type.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
                                break;
                            case 'n':
                            case 'N':
                                idi.Add(ulong.Parse(arg.Substring(1), NumberStyles.HexNumber));
                                break;
                            case 'a':
                                apms.Add(arg.Substring(1).ToLowerInvariant());
                                break;
                            case 's':
                                {
                                    int v = int.Parse(arg.Substring(1), NumberStyles.Number);
                                    if (t[2] == null) {
                                        t[2] = v;
                                    } else if (v > (int)t[2]) {
                                        t[2] = v;
                                    }
                                }
                                break;
                            case 'S':
                                {
                                    int v = int.Parse(arg.Substring(1), NumberStyles.Number);
                                    if (t[3] == null) {
                                        t[3] = v;
                                    } else if (v < (int)t[3]) {
                                        t[3] = v;
                                    }
                                }
                                break;
                        }
                    } finally {
                        t[0] = id;
                        t[1] = type;
                        t[4] = apms;
                        t[5] = idi;
                        query = t;
                    }
                }
            } else if (flag[0] == 'l') {
                object[] t = new object[2] { null, null };
                List<ulong> pkg = new List<ulong>();
                List<string> content = new List<string>();
                for (int i = 2; i < args.Length; ++i) {
                    string arg = args[i];
                    try {
                        switch (arg[0]) {
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
            } else if (flag[0] == 'a') {
                all_langs = true;
            } else if (flag[0] == 't') {
                query = new object[1] { new HashSet<ulong>() };
            } else {
                Console.Error.WriteLine("Unsupported operand {0}", flag[0]);
                return;
            }

            CASCConfig config = CASCConfig.LoadLocalStorageConfig(root, true, !all_langs);
            CASCHandler handler = CASCHandler.OpenStorage(config);
            OwRootHandler ow = handler.Root as OwRootHandler;
            if (ow == null) {
                Console.Error.WriteLine("Not a valid Overwatch installation");
                return;
            }

            if (flag[0] == 'a') {
                foreach (string name in ow.APMList) {
                    Console.Out.WriteLine(name);
                }
                return;
            }

            foreach (APMFile apm in ow.APMFiles) {
                string apmName = System.IO.Path.GetFileName(apm.Name);
                if (flag[0] == 'f' && query[4] != null && ((List<string>)query[4]).Count > 0 && !((List<string>)query[4]).Contains(apmName.ToLowerInvariant())) {
                    continue;
                }

                if (flag[0] == 'C') {
                    foreach (ulong key in apm.CMFMap.Keys) {
                        ulong rtype = GUID.Type(key);
                        ulong rindex = GUID.LongKey(key);
                        ulong rindex2 = GUID.Index(key);

                        bool check1 = ((List<ulong>)query[0]).Count == 0;
                        bool check2 = ((List<ulong>)query[1]).Count == 0;
                        bool check5 = ((List<ulong>)query[5]).Count == 0;

                        if (((List<ulong>)query[0]).Count > 0 && ((List<ulong>)query[0]).Contains(rindex)) { // if index is not in i
                            check1 = true;
                        }
                        if (((List<ulong>)query[1]).Count > 0 && ((List<ulong>)query[1]).Contains(rtype)) { // if type is not in t
                            check2 = true;
                        }
                        if (((List<ulong>)query[5]).Count > 0 && ((List<ulong>)query[5]).Contains(rindex2)) { // if type is not in t
                            check5 = true;
                        }

                        bool check = check1 && check2 && check5;
                        if (check) {
                            Console.Out.WriteLine("Found {0:X12}.{1:X3} in APM {2}", rindex, rtype, apm.Name);
                        }
                    }
                } else if (flag[0] == 't') {
                    foreach (ulong key in apm.CMFMap.Keys) {
                        ((HashSet<ulong>)query[0]).Add(key >> 48);
                    }
                }

                if (flag[0] == 'C' || flag[0] == 't') {
                    continue;
                }

                for (long i = 0; i < apm.Packages.LongLength; ++i) {
                    APMPackage package = apm.Packages[i];
                    PackageIndexRecord[] records = apm.Records[i];

                    if (flag[0] == 'l') {
                        if (((List<ulong>)query[0]).Count + ((List<string>)query[1]).Count == 0) {
                            return;
                        }

                        bool ret = true;
                        if (((List<ulong>)query[0]).Count > 0 && ((List<ulong>)query[0]).Contains(package.packageKey)) {
                            ret = false;
                        }

                        if (ret && ((List<string>)query[1]).Count > 0 && ((List<string>)query[1]).Contains(package.indexContentKey.ToHexString().ToUpperInvariant())) {
                            ret = false;
                        }

                        if (ret) {
                            continue;
                        }

                      ((List<ulong>)query[0]).Remove(package.packageKey);
                        ((List<string>)query[1]).Remove(package.indexContentKey.ToHexString().ToUpperInvariant());

                        Console.Out.WriteLine("Dump for package i{0} / p{1:X} in APM {2}", package.indexContentKey.ToHexString().ToUpperInvariant(), package.packageKey, apm.Name);
                    }

                    for (long j = 0; j < records.LongLength; ++j) {
                        PackageIndexRecord record = records[j];

                        ulong rtype = GUID.Type(record.Key);
                        ulong rindex = GUID.LongKey(record.Key);
                        ulong rindex2 = GUID.Index(record.Key);

                        if (flag[0] == 'f') {
                            bool check1 = ((List<ulong>)query[0]).Count == 0;
                            bool check2 = ((List<ulong>)query[1]).Count == 0;
                            bool check3 = query[2] == null;
                            bool check4 = query[3] == null;
                            bool check5 = ((List<ulong>)query[5]).Count == 0;

                            if (((List<ulong>)query[0]).Count > 0 && ((List<ulong>)query[0]).Contains(rindex)) { // if index is not in i
                                check1 = true;
                            }
                            if (((List<ulong>)query[1]).Count > 0 && ((List<ulong>)query[1]).Contains(rtype)) { // if type is not in t
                                check2 = true;
                            }
                            if (query[2] != null && (int)query[2] > record.Size) { // if size is less than s[lt]
                                check3 = true;
                            }
                            if (query[3] != null && (int)query[3] < record.Size) { // if size is greater than s[gt]
                                check4 = true;
                            }
                            if (((List<ulong>)query[5]).Count > 0 && ((List<ulong>)query[5]).Contains(rindex2)) { // if type is not in t
                                check5 = true;
                            }
                            bool check = check1 && check2 && check3 && check4 && check5;
                            if (check) {
                                Console.Out.WriteLine("Found {0:X12}.{1:X3} in package p{2:X} in APM {3}", rindex, rtype, package.packageKey, apm.Name);
                            }
                        } else if (flag[0] == 'l') {
                            Console.Out.WriteLine("\t{0:X12}.{1:X3} ({2} bytes) - {3:X}", rindex, rtype, record.Size, record.Key);
                        }
                    }
                }
            }
            if (flag[0] == 't') {
                foreach (ulong type in (HashSet<ulong>)query[0]) {
                    byte[] be = BitConverter.GetBytes((ushort)type);
                    Array.Reverse(be);
                    Console.Out.WriteLine("{2:X4} : {0:X4} : {1:X3}", (ushort)type, GUID.Type(type << 48), BitConverter.ToUInt16(be, 0));
                }
            }
        }
    }
}
