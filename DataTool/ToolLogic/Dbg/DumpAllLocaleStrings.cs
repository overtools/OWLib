using System;
using System.Collections.Generic;
using System.IO;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.JSON;
using DataTool.ToolLogic.List;
using TACTLib.Client;
using TACTLib.Client.HandlerArgs;
using TACTLib.Core.Product.Tank;
using TankLib;
using TankLib.Helpers;
using static DataTool.Program;

namespace DataTool.ToolLogic.Dbg {
    [Tool("dbg-dump-localized-names", Description = "Dump strings for all languages", CustomFlags = typeof(ListFlags), IsSensitive = true, UtilNoArchiveNeeded = true)]
    public class DumpLocalizedNames : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            if (toolFlags.Positionals.Length < 3) {
                throw new Exception("no output path");
            }

            var basePath = toolFlags.Positionals[2];
            var output = new List<string>();

            foreach (var language in ValidLanguages) {
                try {
                    InitStorage(language);
                    output.Add($";{language},,");

                    foreach (var key in TrackedFiles[0x75]) {
                        var hero = new Hero(key);
                        if (!hero.IsHero) continue;

                        output.Add($"{teResourceGUID.Index(key):X},{teResourceGUID.Type(key):X},{hero.Name.Trim().ToLowerInvariant()}");
                    }
                } catch (Exception) {
                    // ignored
                }
            }

            File.WriteAllLines(Path.Combine(basePath, "localization.csv"), output);
        }


        private static void InitStorage(string language) {
            Logger.Info("CASC", $"Attempting to load language {language}");
            Client = null;
            TankHandler = null;
            TrackedFiles = null;

            var args = new ClientCreateArgs {
                SpeechLanguage = "enUS",
                TextLanguage = language,
                HandlerArgs = new ClientCreateArgs_Tank {CacheAPM = Flags.UseCache},
                Online = false
            };

            Client = new ClientHandler(Flags.OverwatchDirectory, args);
            TankHandler = Client.ProductHandler as ProductHandler_Tank;

            if (TankHandler == null) {
                Logger.Error("Core", $"Not a valid Overwatch installation (detected product: {Client.Product})");
                return;
            }

            InitTrackedFiles();
            InitKeys();
        }
    }
}
