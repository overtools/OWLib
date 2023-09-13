using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using DataTool.ConvertLogic.WEM;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.SaveLogic;
using DataTool.ToolLogic.Extract;
using Microsoft.Win32;
using TankLib;
using TankLib.TACT;
using TACTLib.Client;
using TACTLib.Client.HandlerArgs;
using TACTLib.Core.Product.Tank;
using TACTLib.Exceptions;
using TankLib.Helpers;
using ValveKeyValue;
using static DataTool.Helper.Logger;
using Logger = TankLib.Helpers.Logger;
using static DataTool.Helper.SpellCheckUtils;

namespace DataTool {
    public static class Program {
        public static ClientHandler Client;
        public static ProductHandler_Tank TankHandler;
        public static Dictionary<ushort, HashSet<ulong>> TrackedFiles;
        public static ToolFlags Flags;
        public static uint BuildVersion;
        public static bool IsPTR => Client?.ProductCode == "prot";
        public static bool IsBeta => Client?.ProductCode == "prob";

        public static readonly string[] ValidLanguages = { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "thTH", "trTR", "zhCN", "zhTW" };

        private static readonly Dictionary<string, string> SteamLocaleMapping = new()  {
            { "german", "deDE" },
            { "english", "enUS" },
            { "spanish", "esES" },
            { "latam", "esMX" },
            { "french", "frFR" },
            { "italian", "itIT" },
            { "japanese", "jaJP" },
            { "koreana", "koKR" },
            { "polish", "plPL" },
            { "portuguese", "ptBR" },
            { "russian", "ruRU" },
            { "thai", "thTH" },
            { "turkish", "trTR" },
            { "schinese", "zhCN" },
            { "tchinese", "zhTW" },
        };

        public static void Main() {
            InitTankSettings();
            HookConsole();

            var tools = GetTools();

        #if DEBUG
            FlagParser.CheckCollisions(typeof(ToolFlags), (flag, duplicate) => {
                Logger.Error("Flag", $"The flag \"{flag}\" from {duplicate} is a duplicate!");
            });
        #endif

            FlagParser.LoadArgs();

            Logger.Info("Core", $"{Assembly.GetExecutingAssembly().GetName().Name} v{Util.GetVersion(typeof(Program).Assembly)}");

            Logger.Info("Core", $"CommandLine: [{string.Join(", ", FlagParser.AppArgs.Select(x => $"\"{x}\""))}]");

            Flags = FlagParser.Parse<ToolFlags>(full => PrintHelp(full, tools));
            if (Flags == null)
                return;

            if (Flags.OverwatchDirectory == "{overwatch_directory}") {
                Logger.Error("Core", "You need to replace {overwatch_directory} with the location you have Overwatch installed to. It can be found in Battle.net. Remember to surround it with quotes");
                return;
            }

            // todo: this code cant detect e.g `"c:\mypath\" list-heroes` because flags validation fails
            if (Flags.OverwatchDirectory.EndsWith("\"")) {
                Logger.Error("Core", "The Overwatch directory you passed will confuse the tool! Please remove the last \\ character");
                return;
            }

            if (Flags.OverwatchDirectory.StartsWith("{") || Flags.OverwatchDirectory.EndsWith("}")) {
                Logger.Error("Core", "Do not include { or } in the Overwatch directory you pass to the tool. The path should be surrounded with quotation marks only");
                return;
            }

            // Overrides the Overwatch Directory with the path set in the Environment variable.
            // Useful for debugging as you can use saved configurations without having to edit the directory saved in the arguments
            var overwatchDirectoryOverride = Environment.GetEnvironmentVariable("DATATOOL_OVERWATCH_DIR");
            if (!string.IsNullOrEmpty(overwatchDirectoryOverride)) {
                Flags.OverwatchDirectory = overwatchDirectoryOverride;
            }

            Logger.Info("Core", $"CommandLineFile: {FlagParser.ArgFilePath}");

            if (Flags.SaveArgs) {
                FlagParser.AppArgs = FlagParser.AppArgs.Where(x => !x.StartsWith("--arg")).ToArray();
                FlagParser.SaveArgs(Flags.OverwatchDirectory);
            } else if (Flags.ResetArgs || Flags.DeleteArgs) {
                FlagParser.ResetArgs();

                if (Flags.DeleteArgs)
                    FlagParser.DeleteArgs();

                Logger.Info("Core", $"CommandLineNew: [{string.Join(", ", FlagParser.AppArgs.Select(x => $"\"{x}\""))}]");
                Flags = FlagParser.Parse<ToolFlags>(full => PrintHelp(full, tools));
                if (Flags == null)
                    return;
            }

            if (string.IsNullOrWhiteSpace(Flags.OverwatchDirectory) || string.IsNullOrWhiteSpace(Flags.Mode) || Flags.Help) {
                PrintHelp(true, tools);
                return;
            }

            // find the current tool/mode to run
            var (targetTool, targetToolFlags, targetToolAttributes) = GetToolActivation(tools);
            if (targetTool == null) {
                return;
            }

            if (targetToolFlags is ExtractFlags extractFlags) {
                if (extractFlags.OutputPath.EndsWith("\"")) {
                    Logger.Error("Core", "The output directory you passed will confuse the tool! Please remove the last \\ character");
                    return;
                }

                if (extractFlags.OutputPath == "{output_directory}") {
                    Logger.Error("Core", "You need to replace {output_directory} with where you want the output files to go. Can just be something like \"out\" and a folder will be created next to the tool");
                    return;
                }

                if (extractFlags.OutputPath.StartsWith("{") || extractFlags.OutputPath.EndsWith("}")) {
                    Logger.Error("Core", "Do not include { or } in the output directory you pass to the tool. The path should be surrounded with quotation marks only");
                    return;
                }
            }

            if (!targetToolAttributes.UtilNoArchiveNeeded) {
                try {
                    InitStorage(Flags.Online);
                } catch (Exception ex) when (ex.InnerException is UnsupportedBuildVersionException) {
                    Logger.Log24Bit(ConsoleSwatch.XTermColor.OrangeRed, true, Console.Error, "CASC", "This version of DataTool does not support this version of Overwatch.");
                    Logger.Log24Bit(ConsoleSwatch.XTermColor.OrangeRed, true, Console.Error, "CASC", "DataTool must be updated to support every new build of the game. This tool update may not be available straight away.");
                    throw;
                } catch (FileNotFoundException) {
                    // file not found exceptions thrown by TACTLib should already include good exception info, we don't need to log anything here
                    throw;
                } catch {
                    Logger.Log24Bit(ConsoleSwatch.XTermColor.OrangeRed, true, Console.Error, "CASC",
                                    "=================\nError initializing CASC!\n" +
                                    "Please Scan & Repair your game, launch it for a minute, and try the tools again before reporting a bug!\n" +
                                    "========================");

                    throw;
                }

                //foreach (KeyValuePair<ushort, HashSet<ulong>> type in TrackedFiles.OrderBy(x => x.Key)) {
                //    //Console.Out.WriteLine($"Found type: {type.Key:X4} ({type.Value.Count} files)");
                //    Console.Out.WriteLine($"Found type: {type.Key:X4}");
                //}

                InitKeys();
                InitMisc();
            }

            var stopwatch = new Stopwatch();
            Logger.Info("Core", "Tooling...");
            stopwatch.Start();
            targetTool.Parse(targetToolFlags);
            stopwatch.Stop();

            Logger.Success("Core", $"Execution finished in {stopwatch.Elapsed}");

            ShutdownMisc();
        }


        private static void HookConsole() {
            AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
            Process.GetCurrentProcess()
                .EnableRaisingEvents = true;

            AppDomain.CurrentDomain.ProcessExit += (sender, @event) => Console.ForegroundColor = ConsoleColor.Gray;
            Console.CancelKeyPress += (sender, @event) => Console.ForegroundColor = ConsoleColor.Gray;
            Console.OutputEncoding = Encoding.UTF8;
        }

        private static void InitTankSettings() {
            Logger.ShowDebug |= Debugger.IsAttached;
        }

        public static void InitMisc() {
            var dbPath = Flags.ScratchDBPath;
            if (Flags.Deduplicate) {
                Logger.Warn("ScratchDB", "Will attempt to deduplicate files if extracting...");
                if (!string.IsNullOrWhiteSpace(Flags.ScratchDBPath)) {
                    Logger.Warn("ScratchDB", "Loading Scratch database...");
                    if (!File.Exists(dbPath) || new DirectoryInfo(dbPath).Exists)
                        dbPath = Path.Combine(Path.GetFullPath(Flags.ScratchDBPath), "Scratch.db");

                    Combo.ScratchDBInstance.Load(dbPath);
                }
            }

            if (!Flags.NoGuidNames)
                IO.LoadGUIDTable(Flags.OnlyCanonical);

            IO.LoadLocalizedNamesMapping();

            WwiseBank.GetReady();
        }

        public static void ShutdownMisc() {
            SaveScratchDatabase();
        }

        public static void SaveScratchDatabase() {
            if (!string.IsNullOrWhiteSpace(Flags.ScratchDBPath)) {
                var dbPath = Flags.ScratchDBPath;
                if (!File.Exists(dbPath) || new DirectoryInfo(dbPath).Exists)
                    dbPath = Path.Combine(Path.GetFullPath(Flags.ScratchDBPath), "Scratch.db");

                if (Flags.Deduplicate && !string.IsNullOrWhiteSpace(dbPath)) {
                    Logger.Warn("ScratchDB", "Saving Scratch database...");
                    Combo.ScratchDBInstance.Save(dbPath);
                }
            }
        }

        public static void InitStorage(bool online = false) { // turnin offline off again, can cause perf issues with bundle hack
            // Attempt to load language via registry or from Steam, if they were already provided via flags then this won't do anything
            if (!Flags.NoLanguageRegistry) {
                TryFetchLocaleFromSteamInstall(); // fetch from steam first
                TryFetchLocaleFromRegistry();
            }

            Logger.Info("CASC", $"Text Language: {Flags.Language} | Speech Language: {Flags.SpeechLanguage}");

            var args = new ClientCreateArgs {
                SpeechLanguage = Flags.SpeechLanguage,
                TextLanguage = Flags.Language,
                HandlerArgs = new ClientCreateArgs_Tank { ManifestRegion = Flags.RCN ? ProductHandler_Tank.REGION_CN : ProductHandler_Tank.REGION_DEV },
                Online = online,
                RemoteKeyringUrl = "https://raw.githubusercontent.com/overtools/OWLib/master/TankLib/Overwatch.keyring"
            };

            LoadHelper.PreLoad();
            Client = new ClientHandler(Flags.OverwatchDirectory, args);
            LoadHelper.PostLoad(Client);

            if (args.TextLanguage != "enUS")
                Logger.Warn("Core", $"Reminder!! When extracting data in languages other than English, the names of the heroes and skins MUST be in the language you have chosen! ({args.TextLanguage})");

            if (Client.ProductCode != "pro")
                Logger.Warn("Core", $"The branch \"{Client.ProductCode}\" is not supported!. This might result in failure to load. Proceed with caution.");

            if (Client.AgentProduct != null) {
                var clientLanguages = Client.AgentProduct.Settings.Languages.Select(x => x.Language).ToArray();
                var clientLanguagesStr = string.Join(", ", clientLanguages);
                if (!clientLanguages.Contains(args.TextLanguage))
                    Logger.Warn("Core", "Battle.Net Agent reports that text language {0} is not installed. Tool likely will not work correctly. Installed languages: {1}", args.TextLanguage, clientLanguagesStr);
                else if (!clientLanguages.Contains(args.SpeechLanguage))
                    Logger.Warn("Core", "Battle.Net Agent reports that speech language {0} is not installed. Installed languages: {1}", args.TextLanguage, clientLanguagesStr);
            }

            TankHandler = Client.ProductHandler as ProductHandler_Tank;
            if (TankHandler == null) {
                Logger.Error("Core", $"Not a valid Overwatch installation (detected product: {Client.Product})");
                return;
            }

            // todo: these version checks don't really need to even exist anymore but whatever, maybe useful just for history - js
            Client.InstallationInfo.Values.TryGetValue("Version", out var clientVersion);
            string buildVersion;
            if (clientVersion != null && clientVersion.Contains('.')) {
                buildVersion = clientVersion.Split('.').LastOrDefault();
            } else {
                buildVersion = clientVersion?.Split('-').LastOrDefault();
            }

            // Handle cases where build version contains letters e.g. 1.71.1.0.97745a
            buildVersion = Regex.Replace(buildVersion ?? "", "[A-Za-z ]", "");

            if (!uint.TryParse(buildVersion, out BuildVersion))
                Logger.Warn("Core", "Could not parse build version from {0}", clientVersion ?? "null");
            else if (BuildVersion < 39028)
                Logger.Error("Core", "DataTool doesn't support Overwatch versions below 1.14. Please use OverTool.");
            else if (BuildVersion < ProductHandler_Tank.VERSION_152_PTR)
                Logger.Error("Core", "This version of DataTool doesn't support versions of Overwatch below 1.52. Please use older version of tool.");
            else if (BuildVersion < 111387)
                Logger.Error("Core", "This version of DataTool doesn't properly support versions of Overwatch below 2.4. Older version of tool is recommended.");

            InitTrackedFiles();
        }

        public static void InitTrackedFiles() {
            TrackedFiles = new Dictionary<ushort, HashSet<ulong>>();
            foreach (var asset in TankHandler.m_assets) {
                var type = teResourceGUID.Type(asset.Key);
                if (!TrackedFiles.TryGetValue(type, out var typeMap)) {
                    typeMap = new HashSet<ulong>();
                    TrackedFiles[type] = typeMap;
                }

                typeMap.Add(asset.Key);
            }
        }

        public static void InitKeys() {
            Logger.Info("Core", "Checking ResourceKeys");

            // todo: broken for now..
            // surely fix
            /*foreach (var key in TrackedFiles[0x90]) {
                if (!ValidKey(key)) continue;

                var resourceKey = GetInstance<STUResourceKey>(key);
                if (resourceKey == null || resourceKey.GetKeyID() == 0 || Client.ConfigHandler.Keyring.Keys.ContainsKey(resourceKey.GetReverseKeyID())) continue;
                Client.ConfigHandler.Keyring.AddKey(resourceKey.GetReverseKeyID(), resourceKey.m_key);
                Logger.Info("Core", $"Added ResourceKey {resourceKey.GetKeyIDString()}, Value: {resourceKey.GetKeyValueString()}");
            }*/
        }

        private static void TryFetchLocaleFromRegistry() {
            try {
                if (!OperatingSystem.IsWindows()) {
                    return;
                }

                if (Flags.Language == null) {
                    var textLanguage = (string) Registry.GetValue(@"HKEY_CURRENT_USER\Software\Blizzard Entertainment\Battle.net\Launch Options\Pro", "LOCALE", null);
                    if (!string.IsNullOrWhiteSpace(textLanguage)) {
                        if (ValidLanguages.Contains(textLanguage)) {
                            Flags.Language = textLanguage;
                            Logger.Debug("Core", $"Found text language via registry: {textLanguage}");
                        } else {
                            Logger.Error("Core", $"Invalid text language found via registry: {textLanguage}. Ignoring.");
                        }
                    }
                }

                if (Flags.SpeechLanguage == null) {
                    var speechLanguage = (string) Registry.GetValue(@"HKEY_CURRENT_USER\Software\Blizzard Entertainment\Battle.net\Launch Options\Pro", "LOCALE_AUDIO", null);
                    if (!string.IsNullOrWhiteSpace(speechLanguage)) {
                        if (ValidLanguages.Contains(speechLanguage)) {
                            Flags.SpeechLanguage = speechLanguage;
                            Logger.Debug("Core", $"Found speech language via registry: {speechLanguage}");
                        } else {
                            Logger.Error("Core", $"Invalid speech language found via registry: {speechLanguage}. Ignoring.");
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Debug("Core", $"Failed to fetch locale from registry: {ex.Message}");
                // Ignored
            }
        }

        private static void TryFetchLocaleFromSteamInstall() {
            try {
                // already have langugae so no need to lookup
                if (Flags.SpeechLanguage != null && Flags.Language != null) {
                    return;
                }

                // see if the directory is a windows symlink and try to get the real path
                // note: doesn't work on linux/wsl
                var directoryInfo = new DirectoryInfo(Flags.OverwatchDirectory);
                var realOverwatchDirectory = directoryInfo.LinkTarget ?? directoryInfo.FullName; // if it's not a symlink, LinkTarget will be null

                Logger.Debug("Core", $"LinkTarget: {directoryInfo.LinkTarget} | FullName: {directoryInfo.FullName}");
                var appManifestPath = Path.Combine(realOverwatchDirectory, "..", "..", "appmanifest_2357570.acf");
                if (!File.Exists(appManifestPath)) {
                    Logger.Debug("Core", $"Failed to find appmanifest at {appManifestPath}");
                    return;
                }

                // read the appmanifest and get the language
                var stream = File.OpenRead(appManifestPath);
                var appManifest = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);
                var language = appManifest["UserConfig"]?["language"]?.ToString(CultureInfo.InvariantCulture);
                if (language == null) {
                    return;
                }

                // try to map the steam language to a valid language code.
                if (SteamLocaleMapping.TryGetValue(language.ToLower(), out var locale)) {
                    // steam doesn't support seperate text and speech languages, so we just use the same language for both if they aren't already set
                    if (Flags.Language == null) {
                        Flags.Language = locale;
                        Logger.Debug("Core", $"Found text language via Steam install: {locale}");
                    }

                    if (Flags.SpeechLanguage == null) {
                        Flags.SpeechLanguage = locale;
                        Logger.Debug("Core", $"Found speech via Steam install: {locale}");
                    }
                } else {
                    Logger.Error("Core", $"Invalid language found via Steam install: {language}. Ignoring.");
                }
            } catch (Exception ex) {
                Logger.Debug("Core", $"Failed to fetch locale from Steam install: {ex.Message}");
                // Ignored
            }
        }

        private static void HandleSingleException(Exception ex) {
            if (ex is TargetInvocationException fex) {
                ex = fex.InnerException ?? ex;
            }

            Logger.Log24Bit(ConsoleSwatch.XTermColor.HotPink3, true, Console.Error, null, ex.Message);
            Logger.Log24Bit(ConsoleSwatch.XTermColor.MediumPurple, true, Console.Error, null, ex.StackTrace);

            if (ex is BLTEDecoderException decoder) {
                File.WriteAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"BLTEDump-{AppDomain.CurrentDomain.FriendlyName}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.blte"), decoder.GetBLTEData());
            }

            if (ex.InnerException != null) {
                HandleSingleException(ex.InnerException);
            }
        }

        [DebuggerStepThrough]
        private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e) {
            if (e.ExceptionObject is Exception ex) {
                HandleSingleException(ex);

                if (Debugger.IsAttached) throw ex;
            }

            unchecked {
                Environment.Exit(-1);
            }
        }

        #region Tool Initialization

        // returns all the tools/modes that are available
        public static HashSet<Type> GetTools() {
            var tools = new HashSet<Type>();
            {
                var t = typeof(ITool);
                var asm = t.Assembly;
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => p.IsClass && t.IsAssignableFrom(p));

                foreach (var tt in types) {
                    var attribute = tt.GetCustomAttribute<ToolAttribute>();
                    if (tt.IsInterface || attribute == null) continue;

                    tools.Add(tt);
                }
            }

            return tools;
        }

        // returns the tool/mode that should be run based on the flags
        private static (ITool targetTool, ICLIFlags targetToolFlags, ToolAttribute targetToolAttributes) GetToolActivation(HashSet<Type> tools) {
            ITool targetTool = null;
            ICLIFlags targetToolFlags = null;
            ToolAttribute targetToolAttributes = null;

            foreach (var type in tools) {
                var attribute = type.GetCustomAttribute<ToolAttribute>();
                var keywordMatch = string.Equals(attribute.Keyword, Flags.Mode, StringComparison.InvariantCultureIgnoreCase);
                var aliasMatch = attribute.Aliases?.Any(x => string.Equals(x, Flags.Mode, StringComparison.InvariantCultureIgnoreCase)) ?? false;

                if (!keywordMatch && !aliasMatch) {
                    continue;
                }

                targetTool = Activator.CreateInstance(type) as ITool;
                targetToolAttributes = attribute;

                if (attribute.CustomFlags != null) {
                    var flags = attribute.CustomFlags;
                    if (typeof(ICLIFlags).IsAssignableFrom(flags))
                        targetToolFlags = typeof(FlagParser).GetMethod(nameof(FlagParser.Parse), new Type[] { })
                                              ?.MakeGenericMethod(flags)
                                              .Invoke(null, null) as ICLIFlags;
                }

                break;
            }

            if (targetToolFlags == null && targetTool != null) {
                return (null, null, null);
            }

            if (targetTool == null) {
                FlagParser.Help<ToolFlags>(false, new Dictionary<string, string>());
                PrintHelp(false, tools);
                return (null, null, null);
            }

            return (targetTool, targetToolFlags, targetToolAttributes);
        }

        // prints the help message for available tools/modes and flags
        private static void PrintHelp(bool full, IEnumerable<Type> eTools) {
            var tools = new List<Type>(eTools);
            tools.Sort(new ToolComparer());

            Log();
            Log("Modes:");
            Log("  {0, -26} | {1, -40}", "mode", "description");
            Log("".PadLeft(94, '-'));
            foreach (var t in tools) {
                var attribute = t.GetCustomAttribute<ToolAttribute>();
                if (attribute.IsSensitive) continue;

                var desc = attribute.Description;
                if (attribute.Description == null) desc = "";

                Log("  {0, -26} | {1}", attribute.Keyword, desc);
            }

            var flagTypes = new List<Type>();

            foreach (var t in tools) {
                var attribute = t.GetCustomAttribute<ToolAttribute>();
                if (attribute.IsSensitive) continue;

                if (attribute?.CustomFlags == null) continue;
                var flags = attribute.CustomFlags;
                if (!typeof(ICLIFlags).IsAssignableFrom(attribute.CustomFlags)) continue;

                if (flagTypes.Contains(flags)) continue;
                flagTypes.Add(flags);
            }

            foreach (var flagType in flagTypes) {
                var flagInfo = flagType.GetCustomAttribute<FlagInfo>();
                Log();
                Log($"{flagInfo?.Name ?? flagType.Name} Flags - {flagInfo?.Description ?? ""}");
                typeof(FlagParser).GetMethod(nameof(FlagParser.FullHelp))
                    ?.MakeGenericMethod(flagType)
                    .Invoke(null, new object[] { null, true });
            }

            ToolNameSpellCheck();
        }

        internal class ToolComparer : IComparer<Type> {
            public int Compare(Type x, Type y) {
                var xT = (x ?? throw new ArgumentNullException(nameof(x))).GetCustomAttribute<ToolAttribute>();
                var yT = (y ?? throw new ArgumentNullException(nameof(y))).GetCustomAttribute<ToolAttribute>();
                return string.Compare(xT.Keyword, yT.Keyword, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        private static void ToolNameSpellCheck() {
            // this will happen if mode is not found
            if (string.IsNullOrWhiteSpace(Flags?.Mode?.ToLower())) {
                return;
            }

            var symSpell = new SymSpell(50, 6);
            FillToolSpellDict(symSpell);
            SpellCheckString(Flags.Mode.ToLower(), symSpell);
        }

        #endregion
    }
}
