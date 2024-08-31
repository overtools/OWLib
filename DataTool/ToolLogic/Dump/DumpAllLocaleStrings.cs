using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.JSON;
using DataTool.ToolLogic.List;
using TACTLib.Client;
using TACTLib.Client.HandlerArgs;
using TACTLib.Core.Product.Tank;
using TankLib;
using TankLib.Helpers;
using static DataTool.Program;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-all-locale-strings", Description = "Dump strings for all languages", CustomFlags = typeof(ListFlags), IsSensitive = true, UtilNoArchiveNeeded = true)]
    public class DumpStringsLocale : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ListFlags) toolFlags;

            if (!Directory.Exists(Flags.OverwatchDirectory)) {
                throw new DirectoryNotFoundException($"Invalid archive directory. Directory \"{Flags.OverwatchDirectory}\" does not exist. Please specify a valid directory.");
            }

            var data = GetData();
            OutputJSON(data, flags);
        }

        private Dictionary<teResourceGUID, Dictionary<string, string>> GetData() {
            var @return = new Dictionary<teResourceGUID, Dictionary<string, string>>();

            Logger.Log($"Preparing to dump strings for following languages: {string.Join(", ", Program.ValidLanguages)}");
            Logger.Log("You must have the language installed in order for it to be included, languages not installed will be ignored.");

            foreach (var language in Program.ValidLanguages) {
                try {
                    InitStorage(language);

                    foreach (var key in TrackedFiles[0x7C].OrderBy(teResourceGUID.Index)) {
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

            foreach (var guid in @return.Keys.ToArray()) {
                if (@return[guid].Count != 0) {
                    // we got data for some locale
                    continue;
                }

                // no data, e.g encrypted
                @return[guid] = null;
            }

            return @return;
        }

        private static void InitStorage(string language) {
            Logger.Info("CASC", $"Attempting to load language {language}");
            //Client = null; // we will try share cdn indices so don't wipe just yet
            TankHandler = null;
            TrackedFiles = null;

            var args = new ClientCreateArgs {
                SpeechLanguage = "enUS", // doesn't matter, we aren't dumping subtitles/voice
                TextLanguage = language,
                HandlerArgs = new ClientCreateArgs_Tank(),
                Online = true, // could help on bnet if missing stuff :D
                RemoteKeyringUrl = "https://raw.githubusercontent.com/overtools/OWLib/master/TankLib/Overwatch.keyring", // just in case ig
                TryShareCDNIndexWithHandler = Client
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