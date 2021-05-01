using System;
using System.Collections.Generic;
using DataTool.Flag;
using DataTool.JSON;
using TACTLib.Client;
using TACTLib.Client.HandlerArgs;
using TACTLib.Core.Product.Tank;
using TankLib;
using TankLib.Helpers;
using static DataTool.Program;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-all-locale-strings", Description = "Dump strings for all languages", CustomFlags = typeof(DumpFlags), IsSensitive = true, UtilNoArchiveNeeded = true)]
    public class DumpStringsLocale : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();

            if (toolFlags is DumpFlags flags) {
                OutputJSON(data, flags);
            }
        }

        private Dictionary<teResourceGUID, Dictionary<string, string>> GetData() {
            var @return = new Dictionary<teResourceGUID, Dictionary<string, string>>();

            Helper.Logger.Log($"Preparing to dump strings for following languages: {string.Join(", ", Program.ValidLanguages)}");
            Helper.Logger.Log("You must have the language installed in order for it to be included, languages not installed will be ignored.");

            foreach (var language in Program.ValidLanguages) {
                try {
                    InitStorage(language);

                    foreach (var key in TrackedFiles[0x7C]) {
                        var guid = (teResourceGUID) key;
                        if (!@return.ContainsKey(guid))
                            @return[guid] = new Dictionary<string, string>();

                        var str = Helper.IO.GetString(key);
                        if (str == null) continue;

                        @return[guid][language] = str;
                    }
                } catch (Exception) {
                    // ignored
                }
            }

            return @return;
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
