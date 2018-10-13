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
using TankLib;
using TankLib.STU;
using TankLib.STU.Types;
using TACTLib.Client;
using TACTLib.Client.HandlerArgs;
using TACTLib.Core.Product.Tank;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool {
    public class Program {
        public static ClientHandler Client;
        public static ProductHandler_Tank TankHandler;
        public static Dictionary<ushort, HashSet<ulong>> TrackedFiles;

        public static ToolFlags Flags;
        public static uint BuildVersion;
        public static bool IsPTR => Client?.AgentProduct?.Uid == "prometheus_test";

        public static bool ValidKey(ulong key) => TankHandler.Assets.ContainsKey(key);

        private static void Main() {
            InitTankSettings();

            HookConsole();

            #region Tool Detection

            HashSet<Type> tools = new HashSet<Type>();
            {
                Type t = typeof(ITool);
                Assembly asm = t.Assembly;
                List<Type> types = asm.GetTypes().Where(tt => tt.IsClass && t.IsAssignableFrom(tt)).ToList();
                foreach (Type tt in types) {
                    ToolAttribute attribute = tt.GetCustomAttribute<ToolAttribute>();
                    if (tt.IsInterface || attribute == null) {
                        continue;
                    }

                    tools.Add(tt);
                }
            }

            #endregion

        #if DEBUG
            FlagParser.CheckCollisions(typeof(ToolFlags), (flag, duplicate) => { TankLib.Helpers.Logger.Error("Flag", $"The flag \"{flag}\" from {duplicate} is a duplicate!"); });
        #endif

            FlagParser.LoadArgs();

            TankLib.Helpers.Logger.Info("Core", $"{Assembly.GetExecutingAssembly().GetName().Name} v{Util.GetVersion(typeof(Program).Assembly)}");

            TankLib.Helpers.Logger.Info("Core", $"CommandLine: [{string.Join(", ", FlagParser.AppArgs.Select(x => $"\"{x}\""))}]");

            Flags = FlagParser.Parse<ToolFlags>(() => PrintHelp(tools));
            if (Flags == null) {
                return;
            }

            TankLib.Helpers.Logger.Info("Core", $"CommandLineFile: {FlagParser.ArgFilePath}");

            if (Flags.SaveArgs) {
                FlagParser.AppArgs = FlagParser.AppArgs.Where(x => !x.StartsWith("--arg")).ToArray();
                FlagParser.SaveArgs(Flags.OverwatchDirectory);
            } else if (Flags.ResetArgs || Flags.DeleteArgs) {
                FlagParser.ResetArgs();
                if (Flags.DeleteArgs) {
                    FlagParser.DeleteArgs();
                }

                TankLib.Helpers.Logger.Info("Core", $"CommandLineNew: [{string.Join(", ", FlagParser.AppArgs.Select(x => $"\"{x}\""))}]");
                Flags = FlagParser.Parse<ToolFlags>(() => PrintHelp(tools));
                if (Flags == null) {
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(Flags.OverwatchDirectory) || string.IsNullOrWhiteSpace(Flags.Mode)) {
                PrintHelp(tools);
                return;
            }

            ITool targetTool = null;
            ICLIFlags targetToolFlags = null;

            #region Tool Activation

            foreach (Type type in tools) {
                ToolAttribute attribute = type.GetCustomAttribute<ToolAttribute>();

                if (!string.Equals(attribute.Keyword, Flags.Mode, StringComparison.InvariantCultureIgnoreCase)) continue;
                targetTool = Activator.CreateInstance(type) as ITool;

                if (attribute.CustomFlags != null) {
                    Type flags = attribute.CustomFlags;
                    if (typeof(ICLIFlags).IsAssignableFrom(flags)) {
                        targetToolFlags = typeof(FlagParser).GetMethod(nameof(FlagParser.Parse), new Type[] { })?.MakeGenericMethod(flags).Invoke(null, null) as ICLIFlags;
                    }
                }

                break;
            }

            if (targetToolFlags == null) {
                return;
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

            InitStorage();

            //foreach (KeyValuePair<ushort, HashSet<ulong>> type in TrackedFiles.OrderBy(x => x.Key)) {
            //    //Console.Out.WriteLine($"Found type: {type.Key:X4} ({type.Value.Count} files)");
            //    Console.Out.WriteLine($"Found type: {type.Key:X4}");
            //}

            InitKeys();
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

        private static void HookConsole() {
            AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
            Process.GetCurrentProcess().EnableRaisingEvents = true;
            AppDomain.CurrentDomain.ProcessExit += (sender, @event) => Console.ForegroundColor = ConsoleColor.Gray;
            Console.CancelKeyPress += (sender, @event) => Console.ForegroundColor = ConsoleColor.Gray;
            Console.OutputEncoding = Encoding.UTF8;
        }

        private static void InitTankSettings() {
            TankLib.Helpers.Logger.ShowDebug = Debugger.IsAttached;
        }

        public static void InitMisc() {
            var dbPath = Flags.ScratchDBPath;
            if (Flags.Deduplicate) {
                TankLib.Helpers.Logger.Warn("ScratchDB", "Will attempt to deduplicate files if extracting...");
                if (!string.IsNullOrWhiteSpace(Flags.ScratchDBPath)) {
                    TankLib.Helpers.Logger.Warn("ScratchDB", "Loading Scratch database...");
                    if (!File.Exists(dbPath) || new DirectoryInfo(dbPath).Exists) {
                        dbPath = Path.Combine(Path.GetFullPath(Flags.ScratchDBPath), "Scratch.db");
                    }

                    SaveLogic.Combo.ScratchDBInstance.Load(dbPath);
                }
            }

            IO.LoadGUIDTable();
            Sound.WwiseBank.GetReady();
        }

        public static void ShutdownMisc() {
            if (!string.IsNullOrWhiteSpace(Flags.ScratchDBPath)) {
                var dbPath = Flags.ScratchDBPath;
                if (!File.Exists(dbPath) || new DirectoryInfo(dbPath).Exists) {
                    dbPath = Path.Combine(Path.GetFullPath(Flags.ScratchDBPath), "Scratch.db");
                }

                if (Flags.Deduplicate && !string.IsNullOrWhiteSpace(dbPath)) {
                    TankLib.Helpers.Logger.Warn("ScratchDB", "Saving Scratch database...");
                    SaveLogic.Combo.ScratchDBInstance.Save(dbPath);
                }
            }
        }

        public static void InitStorage(bool online = true) {
            if (Flags.Language != null) {
                TankLib.Helpers.Logger.Info("CASC", $"Set language to {Flags.Language}");
            }

            if (Flags.SpeechLanguage != null) {
                TankLib.Helpers.Logger.Info("CASC", $"Set speech language to {Flags.SpeechLanguage}");
            }

            ClientCreateArgs args = new ClientCreateArgs {
                SpeechLanguage = Flags.SpeechLanguage,
                TextLanguage = Flags.Language,
                HandlerArgs = new ClientCreateArgs_Tank {
                    CacheAPM = Flags.UseCache,
                },
                Online = online
            };

            TankLib.TACT.LoadHelper.PreLoad();
            Client = new ClientHandler(Flags.OverwatchDirectory, args);
            TankLib.TACT.LoadHelper.PostLoad(Client);

            if (Client.AgentProduct.Uid != "prometheus") {
                TankLib.Helpers.Logger.Warn("Core", $"The branch \"{Client.AgentProduct.Uid}\" is not supported!. This might result in failure to load. Proceed with caution.");
            }

            if (!Client.AgentProduct.Settings.Languages.Select(x => x.Language).Contains(args.TextLanguage)) {
                TankLib.Helpers.Logger.Warn("Core", "Battle.Net Agent reports that language {0} is not installed.", args.TextLanguage);
            } else if (!Client.AgentProduct.Settings.Languages.Select(x => x.Language).Contains(args.SpeechLanguage)) {
                TankLib.Helpers.Logger.Warn("Core", "Battle.Net Agent reports that language {0} is not installed.", args.SpeechLanguage);
            }

            TankHandler = Client.ProductHandler as ProductHandler_Tank;
            if (TankHandler == null) {
                TankLib.Helpers.Logger.Error("Core", $"Not a valid Overwatch installation (detected product: {Client.Product})");
                return;
            }

            BuildVersion = uint.Parse(Client.InstallationInfo.Values["Version"].Split('.').Last());
            if (BuildVersion < 39028) {
                TankLib.Helpers.Logger.Error("Core", "DataTool doesn't support Overwatch versions below 1.14. Please use OverTool");
            } else if (BuildVersion < 39241) {
                TankLib.Helpers.Logger.Error("Core", "DataTool doesn't support this 1.14 release as it uses un-mangled hashes");
            } else if (BuildVersion < 49154) {
                TankLib.Helpers.Logger.Error("Core", "This version of DataTool doesn't properly support versions below 1.26. Please downgrade DataTool.");
            }

            InitTrackedFiles();
        }

        public static void InitTrackedFiles() {
            TrackedFiles = new Dictionary<ushort, HashSet<ulong>>();
            foreach (KeyValuePair<ulong, ProductHandler_Tank.Asset> asset in TankHandler.Assets) {
                ushort type = teResourceGUID.Type(asset.Key);
                if (!TrackedFiles.TryGetValue(type, out var typeMap)) {
                    typeMap = new HashSet<ulong>();
                    TrackedFiles[type] = typeMap;
                }

                typeMap.Add(asset.Key);
            }
        }

        public static void InitKeys() {
            if (!Flags.SkipKeys) {
                TankLib.Helpers.Logger.Info("Core", "Checking ResourceKeys");

                foreach (ulong key in TrackedFiles[0x90]) {
                    if (!ValidKey(key)) {
                        continue;
                    }

                    STUResourceKey resourceKey = GetInstance<STUResourceKey>(key);
                    if (resourceKey == null || resourceKey.GetKeyID() == 0 || Client.ConfigHandler.Keyring.Keys.ContainsKey(resourceKey.GetReverseKeyID())) continue;
                    Client.ConfigHandler.Keyring.AddKey(resourceKey.GetReverseKeyID(), resourceKey.m_key);
                    TankLib.Helpers.Logger.Info("Core", $"Added ResourceKey {resourceKey.GetKeyIDString()}, Value: {resourceKey.GetKeyValueString()}");
                }
            }
        }

        [DebuggerStepThrough]
        private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e) {
            if (e.ExceptionObject is Exception ex) {
                TankLib.Helpers.Logger.Error(null, ex.ToString());
                if (Debugger.IsAttached) {
                    throw ex;
                }
            }

            unchecked {
                Environment.Exit((int) 0xDEADBEEF);
            }
        }

        internal class ToolComparer : IComparer<Type> {
            public int Compare(Type x, Type y) {
                ToolAttribute xT = (x ?? throw new ArgumentNullException(nameof(x))).GetCustomAttribute<ToolAttribute>();
                ToolAttribute yT = (y ?? throw new ArgumentNullException(nameof(y))).GetCustomAttribute<ToolAttribute>();
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
                ToolAttribute attribute = t.GetCustomAttribute<ToolAttribute>();
                if (attribute.IsSensitive && !Debugger.IsAttached) {
                    continue;
                }

                string desc = attribute.Description;
                if (attribute.Description == null) {
                    desc = "";
                }

                Log("  {0, -23} | {1}", attribute.Keyword, desc);
            }

            Dictionary<string, List<Type>> sortedTools = new Dictionary<string, List<Type>>();

            foreach (Type t in tools) {
                ToolAttribute attribute = t.GetCustomAttribute<ToolAttribute>();
                if (attribute.IsSensitive) continue;
                if (attribute.Keyword == null) continue;
                if (!attribute.Keyword.Contains("-")) continue;

                string result = attribute.Keyword.Split('-').First();

                if (!sortedTools.ContainsKey(result)) sortedTools[result] = new List<Type>();
                sortedTools[result].Add(t);
            }

            foreach (KeyValuePair<string, List<Type>> toolType in sortedTools) {
                Type firstTool = toolType.Value.FirstOrDefault();
                ToolAttribute attribute = firstTool?.GetCustomAttribute<ToolAttribute>();
                if (attribute?.CustomFlags == null) continue;
                Type flags = attribute.CustomFlags;
                if (!typeof(ICLIFlags).IsAssignableFrom(attribute.CustomFlags)) continue;
                Log();
                Log("Flags for {0}-*", toolType.Key);
                typeof(FlagParser).GetMethod(nameof(FlagParser.FullHelp))?.MakeGenericMethod(flags).Invoke(null, new object[] {null, true});
            }
        }
    }
}
