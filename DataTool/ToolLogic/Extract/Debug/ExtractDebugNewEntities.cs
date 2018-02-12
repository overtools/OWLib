using System;
using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using Newtonsoft.Json.Linq;
using STULib.Types.Generic;
using static DataTool.Program;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-newents", Description = "Extract new enities (debug)", TrackTypes = new ushort[] {0x3,0xD,0x8f,0x8e}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
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

        public void AddAdded(Combo.ComboInfo info, List<string> added, ushort type) {
            foreach (ulong key in TrackedFiles[type]) {
                string name = GetFileName(key);
                if (!added.Contains(name)) continue;
                Combo.Find(info, key);
            }
        }
        

        public void ExtractNewEntities(ICLIFlags toolFlags) {
            string basePath;
            
            // sorry if this isn't useful for anyone but me.
            // data.json has a list under the key "added_raw" that contains all of the added files.
            
            //const string dataPath = "D:\\ow\\OverwatchDataManager\\versions\\1.18.1.2.42076\\data.json";
            const string dataPath = "D:\\ow\\OverwatchDataManager\\versions\\1.20.0.2.43435\\data.json";

            List<string> added = GetAddedFiles(dataPath);
            
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            const string container = "DebugNewEntities";
            
            Combo.ComboInfo info = new Combo.ComboInfo();

            Combo.Find(info, 288230376151717278);  // 00000000159E.003
            
            //Combo.Find(info, 0x40000000000146C);  // 00000000146C.003
            //Combo.Find(info, 288230376151716993);  // 000000001481.003
            //Combo.Find(info, 288230376151716994);  // 000000001482.003
            //Combo.Find(info, 288230376151716950);  // 000000001456.003
            //Combo.Find(info, 288230376151716972);  // 00000000146C.003
            //Combo.Find(info, 288230376151716953);  // 000000001459.003
            //Combo.Find(info, 288230376151716948);  // 000000001454.003
            //Combo.Find(info, 288230376151716949);  // 000000001455.003
            //foreach (ulong key in TrackedFiles[0x3]) {
            //    string name = GetFileName(key);
            //    if (!added.Contains(name)) continue;
            //    Combo.Find(info, key);
            //}

            AddAdded(info, added, 0x8F);
            AddAdded(info, added, 0x8E);
            
            SaveLogic.Combo.Save(flags, Path.Combine(basePath, container), info);
        }
    }
}