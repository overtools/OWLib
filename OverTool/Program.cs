using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CASCExplorer;
using OverTool.Flags;
using OWLib;
using OWLib.Types.STUD;

namespace OverTool {
    public struct Record {
        public APMPackage package;
        public PackageIndex index;
        public PackageIndexRecord record;
    }

    class Program {
        public static Dictionary<char, IOvertool> toolsMap;

        static void Main(string[] args) {
            Console.OutputEncoding = Encoding.UTF8;
            List<IOvertool> tools = new List<IOvertool>();

            {
                Assembly asm = typeof(IOvertool).Assembly;
                Type t = typeof(IOvertool);
                List<Type> types = asm.GetTypes().Where(tt => tt != t && t.IsAssignableFrom(tt)).ToList();
                foreach (Type tt in types) {
                    if (tt.IsInterface) {
                        continue;
                    }
                    IOvertool toolinst = (IOvertool)Activator.CreateInstance(tt);
                    if (toolinst.Display) {
                        tools.Add(toolinst);
                    }
                }
            }

            OverToolFlags flags = FlagParser.Parse<OverToolFlags>(() => PrintHelp(tools));
            if (flags == null) {
                return;
            }

            bool quiet = flags.Quiet;

            string root = flags.OverwatchDirectory;
            char opt = flags.Mode[0];

            IOvertool tool = null;
            Dictionary<ushort, List<ulong>> track = new Dictionary<ushort, List<ulong>>();
            track[0x90] = new List<ulong>(); // internal requirements
            toolsMap = new Dictionary<char, IOvertool>();
            foreach (IOvertool t in tools) {
                if (t.Opt == opt) {
                    tool = t;
                }

                foreach (ushort tr in t.Track) {
                    if (!track.ContainsKey(tr)) {
                        track[tr] = new List<ulong>();
                    }
                }
                if (toolsMap.ContainsKey(t.Opt)) {
                    Console.Out.WriteLine("Duplicate opt! {0} conflicts with {1}", t.Title, toolsMap[t.Opt].Title);
                }
                toolsMap[t.Opt] = t;
            }
            if (tool == null || flags.Positionals.Length - 2 < tool.MinimumArgs) {
                PrintHelp(tools);
                return;
            }

            Dictionary<ulong, Record> map = new Dictionary<ulong, Record>();

            Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, OWLib.Util.GetVersion());
            Console.Out.WriteLine("Initializing CASC...");
            Console.Out.WriteLine("Set language to {0}", flags.Language);

            if (flags.SkipKeys) {
                Console.Out.WriteLine("Disabling Key auto-detection...");
            }
            CASCConfig config = CASCConfig.LoadLocalStorageConfig(root, !flags.SkipKeys, false);
            config.Languages = new HashSet<string>(new string[1] { flags.Language });
            Console.Out.WriteLine("Using Overwatch Version {0}", config.BuildName);
            CASCHandler handler = CASCHandler.OpenStorage(config);
            OwRootHandler ow = handler.Root as OwRootHandler;
            if (ow == null) {
                Console.Error.WriteLine("Not a valid overwatch installation");
                return;
            }

            // Fail when trying to extract data from a specified language with 2 or less files found.
            if (ow.APMFiles.Count() == 0) {
                Console.Error.WriteLine("Could not find the files for language {0}. Please confirm that you have that language installed, and are using the names from the target language.", flags.Language);
                return;
            }

            Console.Out.WriteLine("Mapping...");
            foreach (APMFile apm in ow.APMFiles) {
                if (!apm.Name.ToLowerInvariant().Contains("rdev")) {
                    continue; // skip
                }
                //Console.Out.WriteLine("Package Length: {0}", apm.Packages.Length);
                for (int i = 0; i < apm.Packages.Length; ++i) {
                    //Console.Out.WriteLine("i: {0}",i);
                    APMPackage package = apm.Packages[i];
                    //Console.Out.WriteLine("Package: {0}", package);
                    PackageIndex index = apm.Indexes[i];
                    //Console.Out.WriteLine("Package Index: {0}", index);
                    PackageIndexRecord[] records = apm.Records[i];
                    for (long j = 0; j < records.LongLength; ++j) {
                        PackageIndexRecord record = records[j];
                        if (map.ContainsKey(record.Key)) {
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
            foreach (APMFile apm in ow.APMFiles) {
                if (!apm.Name.ToLowerInvariant().Contains("rdev")) {
                    continue;
                }
                foreach (KeyValuePair<ulong, CMFHashData> pair in apm.CMFMap) {
                    ushort id = GUID.Type(pair.Key);
                    if (track.ContainsKey(id)) {
                        track[id].Add(pair.Value.id);
                    }

                    if (map.ContainsKey(pair.Key)) {
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
                    if (handler.Encoding.GetEntry(pair.Value.HashKey, out enc)) {
                        rec.record.Size = enc.Size;
                        map.Add(pair.Key, rec);
                    }
                }
            }
            if (origLength != map.Count) {
                Console.Out.WriteLine("Warning: {0} packageless files", map.Count - origLength);
            }

            if (!flags.SkipKeys) {
                Console.Out.WriteLine("Adding Encryption Keys...");

                foreach (ulong key in track[0x90]) {
                    if (!map.ContainsKey(key)) {
                        continue;
                    }
                    using (Stream stream = Util.OpenFile(map[key], handler)) {
                        if (stream == null) {
                            continue;
                        }
                        STUD stud = new STUD(stream);
                        if (stud.Instances[0].Name != stud.Manager.GetName(typeof(EncryptionKey))) {
                            continue;
                        }
                        EncryptionKey ek = (EncryptionKey)stud.Instances[0];
                        if (!KeyService.keys.ContainsKey(ek.KeyNameLong)) {
                            KeyService.keys.Add(ek.KeyNameLong, ek.KeyValueText.ToByteArray());
                            Console.Out.WriteLine("Added Encryption Key {0}, Value: {1}", ek.KeyNameText, ek.KeyValueText);
                        }
                    }
                }
            }

            Console.Out.WriteLine("Tooling...");

            tool.Parse(track, map, handler, quiet, flags);
            if (System.Diagnostics.Debugger.IsAttached) {
                System.Diagnostics.Debugger.Break();
            }
        }

        internal class OvertoolComparer : IComparer<IOvertool> {
            public int Compare(IOvertool x, IOvertool y) {
                return string.Compare(x.Title, y.Title);
            }
        }

        private static void PrintHelp(List<IOvertool> tools) {
            Console.Out.WriteLine();
            Console.Out.WriteLine("Modes:");
            Console.Out.WriteLine("  m | {0, -30} | {1, -30}", "name", "arguments");
            Console.Out.WriteLine("".PadLeft(64, '-'));
            tools.Sort(new OvertoolComparer());
            foreach (IOvertool t in tools) {
                Console.Out.WriteLine("  {0} | {1,-30} | {2}", t.Opt, t.Title, t.Help);
            }
            return;
        }
    }
}