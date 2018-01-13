using System;
using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.SaveLogic;
using Newtonsoft.Json.Linq;
using STULib.Types.Generic;
using static DataTool.Program;
using static DataTool.Helper.IO;
using Model = DataTool.FindLogic.Model;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-newents", Description = "Extract new enities (debug)", TrackTypes = new ushort[] {0x3}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugNewEntities : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ExtractNewEntities(toolFlags);
        }

        public static List<string> GetAddedFiles(string path) {
            JObject stuTypesJson = JObject.Parse(File.ReadAllText(path));
            
            List<string> added = new List<string>();

            foreach (JToken token in stuTypesJson["added_raw"]) {
                added.Add(Path.GetFileName(token.Value<string>()));
            }
            return added;
        }

        public void ExtractNewEntities(ICLIFlags toolFlags) {
            string basePath;
            
            // sorry if this isn't useful for anyone but me.
            // data.json has a list under the key "added_raw" that contains all of the added files.
            
            const string dataPath = "D:\\ow\\OverwatchDataManager\\versions\\1.18.1.2.42076\\data.json";

            List<string> added = GetAddedFiles(dataPath);
            
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            const string container = "DebugNewEntities";
            
            foreach (ulong key in TrackedFiles[0x3]) {
                string name = GetFileName(key);
                if (!added.Contains(name)) continue;
                HashSet<ModelInfo> models = new HashSet<ModelInfo>();
                models = Model.FindModels(models, new Common.STUGUID(key));

                foreach (ModelInfo model in models) {
                    // SaveLogic.Model.Save(flags, Path.Combine(basePath, container, name, "Models"), model, $"New {key}");
                    // Entity.Save(flags, Path.Combine(basePath, container, name, "Entities"), model.Entities.Values, new Dictionary<ulong, string>());
                }
            }
        }
    }
}