using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using Combo = DataTool.FindLogic.Combo;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-voice-sets", Description = "Extract voice sets", CustomFlags = typeof(ExtractFlags))]
    public class ExtractVoiceSets : QueryParser, ITool {
        private const string Container = "VoiceSets";

        public Dictionary<string, string> QueryNameOverrides => null;
        public List<QueryType> QueryTypes => new List<QueryType>();

        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            flags.EnsureOutputDirectory();

            var parsedTypes = ParseQuery(flags, QueryTypes, QueryNameOverrides);
            if (parsedTypes == null || parsedTypes.First().Key == "*") {
                Log("Saving all voice sets. This will take some time.");
            }

            foreach (var key in TrackedFiles[0x5F]) {
                if (parsedTypes?.Count > 0) {
                    var config = GetQuery(parsedTypes, teResourceGUID.Index(key).ToString("X"), "*");
                    if (config.Count == 0) continue;
                }

                var stu = STUHelper.GetInstance<STUVoiceSet>(key);
                if (stu == null) continue;

                var guidClean = teResourceGUID.AsString(key);
                Log($"Saving VoiceSet: {guidClean}");

                var comboInfo = new Combo.ComboInfo();
                var context = new SaveLogic.Combo.SaveContext(comboInfo);
                Combo.Find(comboInfo, key);
                SaveLogic.Combo.SaveVoiceSet(toolFlags, Path.Combine(flags.OutputPath, Container), context, key);
            }
        }
    }
}
