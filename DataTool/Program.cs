using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CASCLib;
using DataTool.ConvertLogic;
using DataTool.Flag;
using DataTool.Helper;
using OWLib;
using STULib.Types;
using static CASCLib.ApplicationPackageManifest.Types;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;
using Logger = CASCLib.Logger;

namespace DataTool {
    public class Program {
        public static Dictionary<ulong, PackageRecord> Files;
        public static Dictionary<ushort, HashSet<ulong>> TrackedFiles;
        public static CASCHandler CASC;
        public static CASCConfig Config;
        public static OwRootHandler Root;
        public static ToolFlags Flags;
        public static uint BuildVersion;
        public static bool IsPTR;

        public static bool ValidKey(ulong key) => Files.ContainsKey(key);
        
        private static void Main() {
            Console.OutputEncoding = Encoding.UTF8;

            Files = new Dictionary<ulong, PackageRecord>();
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

                    if (attrib.TrackTypes == null) continue;
                    foreach (ushort type in attrib.TrackTypes) {
                        if (!TrackedFiles.ContainsKey(type)) {
                            TrackedFiles[type] = new HashSet<ulong>();
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

            ITool targetTool = null;
            ICLIFlags targetToolFlags = null;

            #region Tool Activation

            foreach (Type type in tools) {
                ToolAttribute attrib = type.GetCustomAttribute<ToolAttribute>();

                if (!string.Equals(attrib.Keyword, Flags.Mode, StringComparison.InvariantCultureIgnoreCase)) continue;
                targetTool = Activator.CreateInstance(type) as ITool;

                if (attrib.CustomFlags != null) {
                    Type flags = attrib.CustomFlags;
                    if (typeof(ICLIFlags).IsAssignableFrom(flags)) {
                        targetToolFlags = typeof(FlagParser).GetMethod("Parse", new Type[] { }).MakeGenericMethod(flags).Invoke(null, null) as ICLIFlags;
                    }
                }
                break;
            }

            #endregion

            if (targetTool == null) {
                FlagParser.Help<ToolFlags>(false);
                PrintHelp(tools);
                if (Debugger.IsAttached) {
                    Debugger.Break();
                }
                return;
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

            foreach (Dictionary<string, string> build in Config.BuildInfo) {
                if (!build.ContainsKey("Tags")) continue;
                if (build["Tags"].Contains("XX?")) IsPTR = true;
                // us ptr region is known as XX, so just look for it in the tags.
                // this should work... untested for Asia
            }
            
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
            IO.MapCMF();
            IO.LoadGUIDTable();
            Sound.WwiseBank.GetReady();

            #region Key Detection
            if (!Flags.SkipKeys) {
                Log("Adding Encryption Keys...");

                foreach (ulong key in TrackedFiles[0x90]) {
                    if (!ValidKey(key)) {
                        continue;
                    }
                    using (Stream stream = IO.OpenFile(Files[key])) {
                        if (stream == null) {
                            continue;
                        }

                        STUEncryptionKey encryptionKey = GetInstance<STUEncryptionKey>(key);
                        if (encryptionKey != null && !KeyService.keys.ContainsKey(encryptionKey.LongRevKey)) {
                            KeyService.keys.Add(encryptionKey.LongRevKey, encryptionKey.KeyValue);
                            Log("Added Encryption Key {0}, Value: {1}", encryptionKey.KeyNameProper, encryptionKey.Key);
                        }
                    }
                }
            }
            #endregion

            Log("Tooling...");
            targetTool.Parse(targetToolFlags);
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

        private static void PrintHelp(IEnumerable<Type> eTools) {
            Log();
            Log("Modes:");
            Log("  {0, -23} | {1, -40}", "mode", "description");
            Log("".PadLeft(94, '-'));
            List<Type> tools = new List<Type>(eTools);
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
                Log("  {0, -23} | {1}", attrib.Keyword, desc);
            }
            
            Dictionary<Type, List<Type>> sortedTools = new Dictionary<Type, List<Type>>();

            foreach (Type t in tools) {
                ToolAttribute attrib = t.GetCustomAttribute<ToolAttribute>();
                if (attrib.Keyword == null) continue;
                if (!attrib.Keyword.Contains("-")) continue;
                if (!typeof(ICLIFlags).IsAssignableFrom(attrib.CustomFlags)) continue;

                if (!sortedTools.ContainsKey(attrib.CustomFlags))
                {
                    sortedTools[attrib.CustomFlags] = new List<Type>();
                }

                sortedTools[attrib.CustomFlags].Add(t);
            }


            foreach (KeyValuePair<Type, List<Type>> toolType in sortedTools.OrderBy(x => x.Key.Name.Length).OrderBy(x => x.Key.Name)) {
                Type flags = toolType.Key;
                Log();

                string child = flags.Name;

                if(flags.BaseType != flags && flags.BaseType != typeof(Object) && !flags.BaseType.IsAbstract && !flags.BaseType.IsInterface)
                {
                    child = $"{child} based on {flags.BaseType.Name}";
                }

                if (toolType.Value.Count > 1)
                {
                    Log("Flags for {0}* ({1})", FindCommonSubstring(toolType.Value.Select(x => x.GetCustomAttribute<ToolAttribute>().Keyword).Where(x => x != null)), child);
                }
                else
                {
                    Log("Flags for {0} ({1})", toolType.Value.First().GetCustomAttribute<ToolAttribute>().Keyword, child);
                }
                typeof(FlagParser).GetMethod("FullHelp").MakeGenericMethod(flags).Invoke(null, new object[] { null, true });
            }
        }

        private static string FindCommonSubstring(IEnumerable<string> enumerable)
        {
            if(enumerable.Count() == 1)
            {
                return enumerable.First();
            }

            string longest = enumerable.OrderBy(x => x.Length).Last();
            HashSet<string> skip = new HashSet<string>();
            for (int i = 0; i < longest.Length; ++i)
            {
                foreach(string str in enumerable)
                {
                    if (!skip.Contains(str) && str[i] != longest[i])
                    {
                        if(i == 0)
                        {
                            skip.Add(str);
                            continue;
                        }
                        return longest.Substring(0, i);
                    }
                }
            }
            return longest;
        }
    }
}
