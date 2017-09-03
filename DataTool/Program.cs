using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CASCExplorer;
using DataTool.Flag;
using DataTool.Helper;
using OWLib;
using STULib;
using STULib.Types;
using static DataTool.Helper.Logger;
using Logger = CASCExplorer.Logger;

namespace DataTool {
    public class Program {
        public static Dictionary<ulong, MD5Hash> Files;
        public static Dictionary<ushort, HashSet<ulong>> TrackedFiles;
        public static CASCHandler CASC;
        public static CASCConfig Config;
        public static OwRootHandler Root;
        public static ToolFlags Flags;
        public static uint BuildVersion = 0;

        public static bool ValidKey(ulong key) => Files.ContainsKey(key);
        
        private static void Main() {
            Console.OutputEncoding = Encoding.UTF8;

            Files = new Dictionary<ulong, MD5Hash>();
            TrackedFiles = new Dictionary<ushort, HashSet<ulong>>();

            #region Tool Detection
            HashSet<Type> tools = new HashSet<Type>();
            {
                Assembly asm = typeof(ITool).Assembly;
                Type t = typeof(ITool);
                List<Type> types = asm.GetTypes().Where(tt => tt != t && t.IsAssignableFrom(tt)).ToList();
                foreach (Type tt in types) {
                    ToolAttribute attrib = tt.GetCustomAttribute<ToolAttribute>();
                    if (tt.IsInterface || attrib == null) {
                        continue;
                    }
                    tools.Add(tt);

                    if (attrib.TrackTypes != null) {
                        foreach (ushort type in attrib.TrackTypes) {
                            if (!TrackedFiles.ContainsKey(type)) {
                                TrackedFiles[type] = new HashSet<ulong>();
                            } 
                        }
                    }
                }
            }
            #endregion

            Flags = FlagParser.Parse<ToolFlags>(() => PrintHelp(tools));
            if (Flags == null) {
                return;
            }

            Logger.EXIT = !Flags.GracefulExit;

            ITool TargetTool = null;
            ICLIFlags TargetToolFlags = null;

            #region Tool Activation

            foreach (Type type in tools) {
                ToolAttribute attrib = type.GetCustomAttribute<ToolAttribute>();

                if (string.Equals(attrib.Keyword, Flags.Mode, StringComparison.InvariantCultureIgnoreCase)) {
                    TargetTool = Activator.CreateInstance(type) as ITool;

                    if (attrib.HasCustomFlags) {
                        Type flags = type.Assembly.GetType(type.FullName + ".Flags");
                        if (flags == null || !typeof(ICLIFlags).IsAssignableFrom(flags)) {
                            continue;
                        }
                        TargetToolFlags = typeof(FlagParser).GetMethod("Parse").MakeGenericMethod(flags).Invoke(null, null) as ICLIFlags;
                    }
                }
            }

            #endregion

            if (TargetTool == null) {
                FlagParser.Help<ToolFlags>(false);
                PrintHelp(tools);
            }

            #region Initialize CASC
            Log("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, Util.GetVersion());
            Log("Initializing CASC...");
            Log("Set language to {0}", Flags.Language);
            CDNIndexHandler.Cache.Enabled = Flags.UseCache;
            CDNIndexHandler.Cache.CacheData = Flags.CacheData;
            CDNIndexHandler.Cache.Validate = Flags.ValidateCache;
            // ngdp:us:pro
            // http:us:pro:us.patch.battle.net:1119
            if (Flags.OverwatchDirectory.ToLowerInvariant().Substring(0, 5) == "ngdp:") {
                string cdn = Flags.OverwatchDirectory.Substring(5, 4);
                string[] parts = Flags.OverwatchDirectory.Substring(5).Split(':');
                string region = "us";
                string product = "pro"; 
                if (parts.Length > 1) {
                    region = parts[1];
                }
                if (parts.Length > 2) {
                    product = parts[2];
                }
                if (cdn == "bnet") {
                    Config = CASCConfig.LoadOnlineStorageConfig(product, region);
                } else {
                    if (cdn == "http") {
                        string host = string.Join(":", parts.Skip(3));
                        Config = CASCConfig.LoadOnlineStorageConfig(host, product, region, true, true, true);
                    }
                }
            } else {
                Config = CASCConfig.LoadLocalStorageConfig(Flags.OverwatchDirectory, !Flags.SkipKeys, false);
            }
            Config.Languages = new HashSet<string>(new[] { Flags.Language });
            #endregion

            BuildVersion = uint.Parse(Config.BuildName.Split('.').Last());

            if (Flags.SkipKeys) {
                Log("Disabling Key auto-detection...");
            }

            Log("Using Overwatch Version {0}", Config.BuildName);
            CASC = CASCHandler.OpenStorage(Config);
            Root = CASC.Root as OwRootHandler;
            if (Root== null) {
                ErrorLog("Not a valid overwatch installation");
                return;
            }

            // Fail when trying to extract data from a specified language with 2 or less files found.
            if (!Root.APMFiles.Any()) {
                ErrorLog("Could not find the files for language {0}. Please confirm that you have that language installed, and are using the names from the target language.", Flags.Language);
                if (!Flags.GracefulExit) {
                    return;
                }
            }

            Log("Mapping...");
            TrackedFiles[0x90] = new HashSet<ulong>();
            CascIO.MapCMF();

            #region Key Detection
            if (!Flags.SkipKeys) {
                Log("Adding Encryption Keys...");

                foreach (ulong key in TrackedFiles[0x90]) {
                    if (!ValidKey(key)) {
                        continue;
                    }
                    using (Stream stream = CascIO.OpenFile(Files[key])) {
                        if (stream == null) {
                            continue;
                        }
                        
                        ISTU stu = ISTU.NewInstance(stream, BuildVersion);
                        STUEncryptionKey ek = stu.Instances.OfType<STUEncryptionKey>().First();
                        if (ek != null && !KeyService.keys.ContainsKey(ek.LongKey)) {
                            KeyService.keys.Add(ek.LongKey, ek.KeyValue);
                            Log("Added Encryption Key {0}, Value: {1}", ek.LongKey, ek.Key);
                        }
                    }
                }
            }
            #endregion

            Log("Tooling...");
            TargetTool?.Parse(TargetToolFlags);
            if (Debugger.IsAttached) {
                Debugger.Break();
            }
        }
        
        internal class ToolComparer : IComparer<Type> {
            public int Compare(Type x, Type y) {
                ToolAttribute xT = x.GetCustomAttribute<ToolAttribute>();
                ToolAttribute yT = y.GetCustomAttribute<ToolAttribute>();
                return string.Compare(xT.Keyword, yT.Keyword, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        private static void PrintHelp(IEnumerable<Type> tools_) {
            Log();
            Log("Modes:");
            Log("  {0, -20} | {1, -40}", "mode", "description");
            Log("".PadLeft(94, '-'));
            List<Type> tools = new List<Type>(tools_);
            tools.Sort(new ToolComparer());
            foreach (Type t in tools) {
                ToolAttribute attrib = t.GetCustomAttribute<ToolAttribute>();
                if (attrib.IsSensitive && !Debugger.IsAttached) {
                    continue;
                }
                string desc = attrib.Description;
                if (attrib.Description == null) {
                    desc = "";
                }
                Log("  {0, -20} | {1}", attrib.Keyword, desc);
            }
            
            foreach (Type t in tools) {
                ToolAttribute attrib = t.GetCustomAttribute<ToolAttribute>();
                if (attrib.HasCustomFlags) {
                    Type flags = t.Assembly.GetType(t.FullName + "+Flags");
                    if (flags == null || !typeof(ICLIFlags).IsAssignableFrom(flags)) {
                        continue;
                    }
                    Log();
                    Log("Flags for {0}", attrib.Keyword);
                    typeof(FlagParser).GetMethod("FullHelp").MakeGenericMethod(flags).Invoke(null, new object[] { null, true });
                }
            }
        }
    }
}
