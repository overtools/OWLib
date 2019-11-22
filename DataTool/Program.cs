using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DataTool.ConvertLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.SaveLogic;
using TankLib;
using TankLib.STU;
using TankLib.STU.Types;
using TankLib.TACT;
using TACTLib.Client;
using TACTLib.Client.HandlerArgs;
using TACTLib.Core.Product.Tank;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;
using Logger = TankLib.Helpers.Logger;

namespace DataTool {
    public class Program {
        public static ClientHandler                      Client;
        public static ProductHandler_Tank                TankHandler;
        public static Dictionary<ushort, HashSet<ulong>> TrackedFiles;

        public static ToolFlags Flags;
        public static uint      BuildVersion;
        public static bool      IsPTR => Client?.AgentProduct?.Uid == "prometheus_test";

        public static bool ValidKey(ulong key) { return TankHandler.Assets.ContainsKey(key); }

        private static void Main() {
            InitTankSettings();

            HookConsole();

            #region Tool Detection

            var tools = new HashSet<Type>();
            {
                var t   = typeof(ITool);
                var asm = t.Assembly;
                var types = asm.GetTypes()
                               .Where(tt => tt.IsClass && t.IsAssignableFrom(tt))
                               .ToList();
                foreach (var tt in types) {
                    var attribute = tt.GetCustomAttribute<ToolAttribute>();
                    if (tt.IsInterface || attribute == null) continue;

                    tools.Add(tt);
                }
            }

            #endregion

        #if DEBUG
            FlagParser.CheckCollisions(typeof(ToolFlags), (flag, duplicate) => { Logger.Error("Flag", $"The flag \"{flag}\" from {duplicate} is a duplicate!"); });
        #endif

            FlagParser.LoadArgs();

            Logger.Info("Core", $"{Assembly.GetExecutingAssembly().GetName().Name} v{Util.GetVersion(typeof(Program).Assembly)}");

            Logger.Info("Core", $"CommandLine: [{string.Join(", ", FlagParser.AppArgs.Select(x => $"\"{x}\""))}]");

            Flags = FlagParser.Parse<ToolFlags>(full => PrintHelp(full, tools));
            if (Flags == null) return;

            Logger.Info("Core", $"CommandLineFile: {FlagParser.ArgFilePath}");

            if (Flags.SaveArgs) {
                FlagParser.AppArgs = FlagParser.AppArgs.Where(x => !x.StartsWith("--arg"))
                                               .ToArray();
                FlagParser.SaveArgs(Flags.OverwatchDirectory);
            } else if (Flags.ResetArgs || Flags.DeleteArgs) {
                FlagParser.ResetArgs();
                if (Flags.DeleteArgs) FlagParser.DeleteArgs();

                Logger.Info("Core", $"CommandLineNew: [{string.Join(", ", FlagParser.AppArgs.Select(x => $"\"{x}\""))}]");
                Flags = FlagParser.Parse<ToolFlags>(full => PrintHelp(full, tools));
                if (Flags == null) return;
            }

            if (string.IsNullOrWhiteSpace(Flags.OverwatchDirectory) || string.IsNullOrWhiteSpace(Flags.Mode) || Flags.Help) {
                PrintHelp(false, tools);
                return;
            }

            ITool     targetTool      = null;
            ICLIFlags targetToolFlags = null;

            #region Tool Activation

            foreach (var type in tools) {
                var attribute = type.GetCustomAttribute<ToolAttribute>();

                if (!string.Equals(attribute.Keyword, Flags.Mode, StringComparison.InvariantCultureIgnoreCase)) continue;
                targetTool = Activator.CreateInstance(type) as ITool;

                if (attribute.CustomFlags != null) {
                    var flags = attribute.CustomFlags;
                    if (typeof(ICLIFlags).IsAssignableFrom(flags))
                        targetToolFlags = typeof(FlagParser).GetMethod(nameof(FlagParser.Parse), new Type[] { })
                                                            ?.MakeGenericMethod(flags)
                                                            .Invoke(null, null) as ICLIFlags;
                }

                break;
            }

            if (targetToolFlags == null) return;

            #endregion

            if (targetTool == null) {
                FlagParser.Help<ToolFlags>(false, new Dictionary<string, string>());
                PrintHelp(false, tools);
                return;
            }

            InitStorage();

            //foreach (KeyValuePair<ushort, HashSet<ulong>> type in TrackedFiles.OrderBy(x => x.Key)) {
            //    //Console.Out.WriteLine($"Found type: {type.Key:X4} ({type.Value.Count} files)");
            //    Console.Out.WriteLine($"Found type: {type.Key:X4}");
            //}

            InitKeys();
            InitMisc();

            var stopwatch = new Stopwatch();
            Logger.Info("Core", "Tooling...");
            stopwatch.Start();
            targetTool.Parse(targetToolFlags);
            stopwatch.Stop();

            Logger.Success("Core", $"Execution finished in {stopwatch.Elapsed} seconds");

            ShutdownMisc();
        }

        private static void HookConsole() {
            AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
            Process.GetCurrentProcess()
                   .EnableRaisingEvents = true;
            AppDomain.CurrentDomain.ProcessExit += (sender, @event) => Console.ForegroundColor = ConsoleColor.Gray;
            Console.CancelKeyPress              += (sender, @event) => Console.ForegroundColor = ConsoleColor.Gray;
            Console.OutputEncoding              =  Encoding.UTF8;
        }

        private static void InitTankSettings() { Logger.ShowDebug = Debugger.IsAttached; }

        public static void InitMisc() {
            var dbPath = Flags.ScratchDBPath;
            if (Flags.Deduplicate) {
                Logger.Warn("ScratchDB", "Will attempt to deduplicate files if extracting...");
                if (!string.IsNullOrWhiteSpace(Flags.ScratchDBPath)) {
                    Logger.Warn("ScratchDB", "Loading Scratch database...");
                    if (!File.Exists(dbPath) || new DirectoryInfo(dbPath).Exists) dbPath = Path.Combine(Path.GetFullPath(Flags.ScratchDBPath), "Scratch.db");

                    Combo.ScratchDBInstance.Load(dbPath);
                }
            }

            IO.LoadGUIDTable();
            Sound.WwiseBank.GetReady();
        }

        public static void ShutdownMisc() {
            if (!string.IsNullOrWhiteSpace(Flags.ScratchDBPath)) {
                var dbPath                                                           = Flags.ScratchDBPath;
                if (!File.Exists(dbPath) || new DirectoryInfo(dbPath).Exists) dbPath = Path.Combine(Path.GetFullPath(Flags.ScratchDBPath), "Scratch.db");

                if (Flags.Deduplicate && !string.IsNullOrWhiteSpace(dbPath)) {
                    Logger.Warn("ScratchDB", "Saving Scratch database...");
                    Combo.ScratchDBInstance.Save(dbPath);
                }
            }
        }

        public static void InitStorage(bool online = true) {
            if (Flags.Language != null) Logger.Info("CASC", $"Set language to {Flags.Language}");

            if (Flags.SpeechLanguage != null) Logger.Info("CASC", $"Set speech language to {Flags.SpeechLanguage}");

            var args = new ClientCreateArgs {
                                                SpeechLanguage = Flags.SpeechLanguage,
                                                TextLanguage   = Flags.Language,
                                                HandlerArgs    = new ClientCreateArgs_Tank { CacheAPM = Flags.UseCache },
                                                Online         = online
                                            };

            LoadHelper.PreLoad();
            Client = new ClientHandler(Flags.OverwatchDirectory, args);
            LoadHelper.PostLoad(Client);

            if (args.TextLanguage != "enUS") Logger.Warn("Core", "Reminder! When extracting data in other languages, the names of the heroes/skins/etc must be in the language you have chosen.");

            if (Client.AgentProduct.ProductCode != "pro") Logger.Warn("Core", $"The branch \"{Client.AgentProduct.ProductCode}\" is not supported!. This might result in failure to load. Proceed with caution.");

            if (!Client.AgentProduct.Settings.Languages.Select(x => x.Language)
                       .Contains(args.TextLanguage))
                Logger.Warn("Core", "Battle.Net Agent reports that language {0} is not installed.", args.TextLanguage);
            else if (!Client.AgentProduct.Settings.Languages.Select(x => x.Language)
                            .Contains(args.SpeechLanguage)) Logger.Warn("Core", "Battle.Net Agent reports that language {0} is not installed.", args.SpeechLanguage);

            TankHandler = Client.ProductHandler as ProductHandler_Tank;
            if (TankHandler == null) {
                Logger.Error("Core", $"Not a valid Overwatch installation (detected product: {Client.Product})");
                return;
            }

            BuildVersion = uint.Parse(Client.InstallationInfo.Values["Version"]
                                            .Split('.')
                                            .Last());
            if (BuildVersion < 39028)
                Logger.Error("Core", "DataTool doesn't support Overwatch versions below 1.14. Please use OverTool.");
            else if (BuildVersion < 56957) 
                Logger.Error("Core", "This version of DataTool doesn't support versions of Overwatch below 1.35. Please downgrade DataTool.");

            InitTrackedFiles();
        }

        public static void InitTrackedFiles() {
            TrackedFiles = new Dictionary<ushort, HashSet<ulong>>();
            foreach (var asset in TankHandler.Assets) {
                var type = teResourceGUID.Type(asset.Key);
                if (!TrackedFiles.TryGetValue(type, out var typeMap)) {
                    typeMap            = new HashSet<ulong>();
                    TrackedFiles[type] = typeMap;
                }

                typeMap.Add(asset.Key);
            }
        }

        public static void InitKeys() {
            if (!Flags.SkipKeys) {
                Logger.Info("Core", "Checking ResourceKeys");

                foreach (var key in TrackedFiles[0x90]) {
                    if (!ValidKey(key)) continue;

                    var resourceKey = GetInstance<STUResourceKey>(key);
                    if (resourceKey == null || resourceKey.GetKeyID() == 0 || Client.ConfigHandler.Keyring.Keys.ContainsKey(resourceKey.GetReverseKeyID())) continue;
                    Client.ConfigHandler.Keyring.AddKey(resourceKey.GetReverseKeyID(), resourceKey.m_key);
                    Logger.Info("Core", $"Added ResourceKey {resourceKey.GetKeyIDString()}, Value: {resourceKey.GetKeyValueString()}");
                }
            }
        }

        [DebuggerStepThrough]
        private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e) {
            if (e.ExceptionObject is Exception ex) {
                Logger.Error(null, ex.ToString());
                if (Debugger.IsAttached) throw ex;
            }

            unchecked {
                Environment.Exit((int) 0xDEADBEEF);
            }
        }

        private static void PrintHelp(bool full, IEnumerable<Type> eTools) {
            var tools = new List<Type>(eTools);
            tools.Sort(new ToolComparer());
            if (!full) {
                Log();
                Log("Modes:");
                Log("  {0, -23} | {1, -40}", "mode", "description");
                Log("".PadLeft(94, '-'));
                foreach (var t in tools) {
                    var attribute = t.GetCustomAttribute<ToolAttribute>();
                    if (attribute.IsSensitive && !Debugger.IsAttached) continue;

                    var desc                                = attribute.Description;
                    if (attribute.Description == null) desc = "";

                    Log("  {0, -23} | {1}", attribute.Keyword, desc);
                }
            }

            var sortedTools = new Dictionary<string, List<Type>>();

            foreach (var t in tools) {
                var attribute = t.GetCustomAttribute<ToolAttribute>();
                if (attribute.IsSensitive) continue;
                if (attribute.Keyword == null) continue;
                if (!attribute.Keyword.Contains("-")) continue;

                var result = attribute.Keyword.Split('-')
                                      .First();

                if (!sortedTools.ContainsKey(result)) sortedTools[result] = new List<Type>();
                sortedTools[result]
                    .Add(t);
            }

            foreach (var toolType in sortedTools) {
                var firstTool = toolType.Value.FirstOrDefault();
                var attribute = firstTool?.GetCustomAttribute<ToolAttribute>();
                if (attribute?.CustomFlags == null) continue;
                var flags = attribute.CustomFlags;
                if (!typeof(ICLIFlags).IsAssignableFrom(attribute.CustomFlags)) continue;
                if (!full) {
                    Log();
                    Log("Flags for {0}-*", toolType.Key);
                    typeof(FlagParser).GetMethod(nameof(FlagParser.FullHelp))
                                      ?.MakeGenericMethod(flags)
                                      .Invoke(null, new object[] { null, true });
                } else {
                    // 
                }
            }
        }

        internal class ToolComparer : IComparer<Type> {
            public int Compare(Type x, Type y) {
                var xT = (x ?? throw new ArgumentNullException(nameof(x))).GetCustomAttribute<ToolAttribute>();
                var yT = (y ?? throw new ArgumentNullException(nameof(y))).GetCustomAttribute<ToolAttribute>();
                return string.Compare(xT.Keyword, yT.Keyword, StringComparison.InvariantCultureIgnoreCase);
            }
        }
    }
}
