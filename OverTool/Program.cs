using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CASCLib;
using OverTool.Flags;
using STULib;
using STULib.Types;

namespace OverTool {
    public struct Record {
        public APMPackage package;
        public PackageIndex index;
        public PackageIndexRecord record;
    }

    class Program {
        public static Dictionary<string, IOvertool> toolsMap;

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

            Logger.EXIT = !flags.GracefulExit;

            bool quiet = flags.Quiet;

            string root = flags.OverwatchDirectory;
            string opt = flags.Mode;

            IOvertool tool = null;
            Dictionary<ushort, List<ulong>> track = new Dictionary<ushort, List<ulong>> {
                [0x90] = new List<ulong>() // internal requirements
            };
            toolsMap = new Dictionary<string, IOvertool>();
            foreach (IOvertool t in tools) {
                if (t.FullOpt == opt || (opt.Length == 1 && t.Opt != (char) 0 && t.Opt == opt[0])) {
                    tool = t;
                }

                if (t.Track != null) {
                    foreach (ushort tr in t.Track) {
                        if (!track.ContainsKey(tr)) {
                            track[tr] = new List<ulong>();
                        }
                    }
                }

                if (toolsMap.ContainsKey(t.FullOpt)) {
                    Console.Out.WriteLine("Duplicate opt! {0} conflicts with {1}", t.Title, toolsMap[t.FullOpt].Title);
                }
                if (t.FullOpt.Length == 1) {
                    Console.Out.WriteLine("FullOpt should not be length 1 for {0}", t.Title);
                }
                toolsMap[t.FullOpt] = t;
            }
            if (tool == null || flags.Positionals.Length - 2 < tool.MinimumArgs) {
                PrintHelp(tools);
                return;
            }

            Dictionary<ulong, Record> map = new Dictionary<ulong, Record>();

            Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, OWLib.Util.GetVersion());
            Console.Out.WriteLine("Initializing CASC...");
            Console.Out.WriteLine("Set language to {0}", flags.Language);
            CDNIndexHandler.Cache.Enabled = flags.UseCache;
            CDNIndexHandler.Cache.CacheData = flags.CacheData;
            CDNIndexHandler.Cache.Validate = flags.ValidateCache;
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
                        config = CASCConfig.LoadOnlineStorageConfig(host, product, region, true, true, true);
                    }
                }
            } else {
                config = CASCConfig.LoadLocalStorageConfig(root, !flags.SkipKeys, false);
            }
            config.Languages = new HashSet<string>(new string[1] { flags.Language });

            if (flags.SkipKeys) {
                Console.Out.WriteLine("Disabling Key auto-detection...");
            }

            Regex versionRegex = new Regex(@"\d+\.\d+");
            Match versionMatch = versionRegex.Match(config.BuildName);

            if (versionMatch.Success) {
                float version = float.Parse(versionMatch.Value);

                if (version > 1.13) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Out.WriteLine("==========\nWARNING: Overtool only works with Overwatch version 1.13 and below! You are using {0}!", config.BuildName);
                    Console.Out.WriteLine("You must use DataTool for Overwatch 1.14 and above!\n==========");
                    Console.ResetColor();
                }
            }

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
                if (!flags.GracefulExit) {
                    return;
                }
            }

            Console.Out.WriteLine("Mapping...");
            Util.MapCMF(ow, handler, map, track, flags.Language);

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
                        ISTU stu = ISTU.NewInstance(stream, UInt32.Parse(config.BuildName.Split('.').Last()));
                        if (!(stu.Instances.FirstOrDefault() is STUEncryptionKey)) {
                            continue;
                        }
                        STUEncryptionKey ek = stu.Instances.FirstOrDefault() as STUEncryptionKey;
                        if (ek != null && !KeyService.keys.ContainsKey(ek.LongRevKey)) {
                            KeyService.keys.Add(ek.LongRevKey, ek.KeyValue);
                            Console.Out.WriteLine("Added Encryption Key {0}, Value: {1}", ek.KeyNameProper, ek.Key);
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
            Console.Out.WriteLine("  {0, -20} | {1, -40} | {2, -30}", "mode", "name", "arguments");
            Console.Out.WriteLine("".PadLeft(94, '-'));
            tools.Sort(new OvertoolComparer());
            foreach (IOvertool t in tools) {
                string help = t.Help;
                if (help == null) {
                    help = "No additional arguments";
                }
                Console.Out.WriteLine("  {0, -20} | {1,-40} | {2}", t.FullOpt, t.Title, help);
            }
            return;
        }
    }
}