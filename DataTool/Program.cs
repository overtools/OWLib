using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CMFLib;
using DataTool.ConvertLogic;
using DataTool.Flag;
using DataTool.Helper;
using TankLib.CASC;
using TankLib.CASC.Handlers;
using TankLib.CASC.Helpers;
using TankLib.STU;
using TankLib.STU.Types;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool {
    public class Program {
        public static Dictionary<ulong, ApplicationPackageManifest.Types.PackageRecord> Files;
        public static Dictionary<ulong, ContentManifestFile.HashData> CMFMap;
        public static Dictionary<ushort, HashSet<ulong>> TrackedFiles;
        public static CASCHandler CASC;
        public static CASCConfig Config;
        public static RootHandler Root;
        public static ToolFlags Flags;
        public static uint BuildVersion;
        public static bool IsPTR => Config?.IsPTR == true;

        public static bool ValidKey(ulong key) => Files.ContainsKey(key);
        
        private static void Main() {
            AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
            Process.GetCurrentProcess().EnableRaisingEvents = true;
            AppDomain.CurrentDomain.ProcessExit += (sender, @event) => Console.ForegroundColor = ConsoleColor.Gray;
            Console.CancelKeyPress += (sender, @event) => Console.ForegroundColor = ConsoleColor.Gray;
            Console.OutputEncoding = Encoding.UTF8;

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
                }
            }
            #endregion

            Flags = FlagParser.Parse<ToolFlags>(() => PrintHelp(tools));
            if (Flags == null) {
                return;
            }

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
                        targetToolFlags = typeof(FlagParser).GetMethod(nameof(FlagParser.Parse), new Type[] { }).MakeGenericMethod(flags).Invoke(null, null) as ICLIFlags;
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
            
            TankLib.Helpers.Logger.Info("Core", $"{Assembly.GetExecutingAssembly().GetName().Name} v{TankLib.Util.GetVersion(typeof(Program).Assembly)}");
            TankLib.Helpers.Logger.Info("Core", $"CommandLine: [{string.Join(", ", Environment.GetCommandLineArgs().Skip(1).Select(x => $"\"{x}\""))}]");

            InitStorage();
            
            //foreach (KeyValuePair<ushort, HashSet<ulong>> type in TrackedFiles.OrderBy(x => x.Key)) {
            //    //Console.Out.WriteLine($"Found type: {type.Key:X4} ({type.Value.Count} files)");
            //    Console.Out.WriteLine($"Found type: {type.Key:X4}");
            //}

            #region Key Detection
            if (!Flags.SkipKeys) {
                TankLib.Helpers.Logger.Info("Core", "Checking ResourceKeys");

                foreach (ulong key in TrackedFiles[0x90]) {
                    if (!ValidKey(key)) {
                        continue;
                    }
                    using (Stream stream = IO.OpenFile(Files[key])) {
                        if (stream == null) {
                            continue;
                        }

                        STUResourceKey resourceKey = GetInstance<STUResourceKey>(key);
                        if (resourceKey == null || resourceKey.GetKeyID() == 0 || TACTKeyService.Keys.ContainsKey(resourceKey.GetReverseKeyID())) continue;
                        TACTKeyService.Keys.Add(resourceKey.GetReverseKeyID(), resourceKey.m_key);
                        TankLib.Helpers.Logger.Info("Core", $"Added ResourceKey {resourceKey.GetKeyIDString()}, Value: {resourceKey.GetKeyValueString()}");
                    }
                }
            }
            #endregion
            
            InitMisc();
            
            Stopwatch stopwatch = new Stopwatch();
            TankLib.Helpers.Logger.Info("Core", "Tooling...");
            stopwatch.Start();
            targetTool.Parse(targetToolFlags);
            stopwatch.Stop();
            
            TankLib.Helpers.Logger.Success("Core", $"Execution finished in {stopwatch.Elapsed} seconds");

            ShutdownMisc();

            if (Debugger.IsAttached) {
                Debugger.Break();
            }
        }
        
        public static void InitMisc() {
            var dbPath = Flags.ScratchDBPath;
            if (Flags.Deduplicate) {
                TankLib.Helpers.Logger.Warn("ScratchDB", "Will attempt to deduplicate files if extracting...");
                if(!string.IsNullOrWhiteSpace(Flags.ScratchDBPath)) {
                    TankLib.Helpers.Logger.Warn("ScratchDB", "Loading deduplication database...");
                    if (!File.Exists(dbPath)) {
                        dbPath = Path.Combine(Path.GetFullPath(Flags.ScratchDBPath), "Scratch.db");
                    }
                    SaveLogic.Combo.ScratchDBInstance.Load(dbPath);
                }
            }
            
            IO.LoadGUIDTable();
            Sound.WwiseBank.GetReady();
        }

        public static void ShutdownMisc() {
            var dbPath = Flags.ScratchDBPath;
            if(Flags.Deduplicate && !string.IsNullOrWhiteSpace(dbPath)) {
                TankLib.Helpers.Logger.Warn("ScratchDB", "Saving deduplication database...");
                SaveLogic.Combo.ScratchDBInstance.Save(dbPath);
            }
        }

        public static void InitStorage() {
            Files = new Dictionary<ulong, ApplicationPackageManifest.Types.PackageRecord>();
            TrackedFiles = new Dictionary<ushort, HashSet<ulong>>();
            
            if (Flags.Language != null)
            {
                TankLib.Helpers.Logger.Info("CASC", $"Set language to {Flags.Language}");
            }
            if (Flags.SpeechLanguage != null)
            {
                TankLib.Helpers.Logger.Info("CASC", $"Set speech language to {Flags.SpeechLanguage}");
            }

            CASCHandler.Cache.CacheAPM = Flags.UseCache;
            CASCHandler.Cache.CacheCDN = Flags.UseCache;
            CASCHandler.Cache.CacheCDNData = Flags.CacheCDNData;
            Config = CASCConfig.LoadFromString(Flags.OverwatchDirectory, Flags.SkipKeys);
            Config.SpeechLanguage = Flags.SpeechLanguage ?? Flags.Language ?? Config.SpeechLanguage;
            Config.TextLanguage = Flags.Language ?? Config.TextLanguage;

            if (Config.InstallData != null) {
                if (!Config.InstallData.Settings.Languages.Select(x => x.Language).Contains(Config.TextLanguage)) {
                    TankLib.Helpers.Logger.Warn("Core", "Battle.Net Agent reports that language {0} is not installed.", Config.TextLanguage);
                } else if (!Config.InstallData.Settings.Languages.Select(x => x.Language).Contains(Config.SpeechLanguage)) {
                    TankLib.Helpers.Logger.Warn("Core", "Battle.Net Agent reports that language {0} is not installed.", Config.SpeechLanguage);
                }

                if (Config.InstallData.Uid != "prometheus") {
                    TankLib.Helpers.Logger.Warn("Core", $"The branch \"{Config.InstallData.Uid}\" is not supported!. This might result in failure to load. Proceed with caution.");
                }
            }

            BuildVersion = uint.Parse(Config.BuildVersion.Split('.').Last());

            if (BuildVersion < 39028) {
                TankLib.Helpers.Logger.Error("Core", "DataTool doesn't support Overwatch versions below 1.14. Please use OverTool");
            } else if (BuildVersion < 39241) {
                TankLib.Helpers.Logger.Error("Core", "DataTool doesn't support this 1.14 release as it uses unmangeled hashes");
            } else if (BuildVersion < 49154) {
                TankLib.Helpers.Logger.Error("Core", "This version of DataTool doesn't properly support versions below 1.26. Please downgrade DataTool.");
            }

            TankLib.Helpers.Logger.Info("Core", $"Using Overwatch Version {Config.BuildVersion}");
            TankLib.Helpers.Logger.Info("CASC", "Initializing...");
            CASC = CASCHandler.Open(Config);
            Root = CASC.RootHandler;
            //if (Root== null) {
            //    ErrorLog("Not a valid overwatch installation");
            //    return;
            //}
            
            // Fail when trying to extract data from a specified language with 2 or less files found.
            if (!Root.APMFiles.Any()) {
                TankLib.Helpers.Logger.Error("Core", "Unable to load APM files for language {0}. Please confirm that you have that language installed.", Flags.Language);
                    return;
            }

            TankLib.Helpers.Logger.Info("Core", "Mapping storage");
            TrackedFiles[0x90] = new HashSet<ulong>();
            
            using (PerfCounter _ = new PerfCounter("DataTool.Helper.IO::MapCMF"))
            IO.MapCMF();
        }

        [DebuggerStepThrough]
        private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e) {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null) {
                TankLib.Helpers.Logger.Error(null, ex.ToString());
                if (Debugger.IsAttached) {
                    throw ex;
                }
            }
            unchecked {
                Environment.Exit((int)0xDEADBEEF);
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

            Dictionary<string, List<Type>> sortedTools = new Dictionary<string, List<Type>>();
            
            foreach (Type t in tools) {
                ToolAttribute attrib = t.GetCustomAttribute<ToolAttribute>();
                if (attrib.Keyword == null) continue;
                if (!attrib.Keyword.Contains("-")) continue;
                
                string result = attrib.Keyword.Split('-').First();
                
                if (!sortedTools.ContainsKey(result)) sortedTools[result] = new List<Type>();
                sortedTools[result].Add(t);
            }

            foreach (KeyValuePair<string,List<Type>> toolType in sortedTools) {
                Type firstTool = toolType.Value.FirstOrDefault();
                ToolAttribute attrib = firstTool?.GetCustomAttribute<ToolAttribute>();
                if (attrib?.CustomFlags == null) continue;
                Type flags = attrib.CustomFlags;
                if (!typeof(ICLIFlags).IsAssignableFrom(attrib.CustomFlags)) continue;
                Log();
                Log("Flags for {0}-*", toolType.Key);
                typeof(FlagParser).GetMethod(nameof(FlagParser.FullHelp)).MakeGenericMethod(flags).Invoke(null, new object[] { null, true });
            }
        }
    }
}
