using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;
using DataTool.ToolLogic.List;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-strings", Description = "Dump strings", CustomFlags = typeof(ListFlags))]
    public class DumpStrings : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var strings = GetStrings();

            if (toolFlags is ListFlags flags) {
                if (flags.JSON) {
                    OutputJSON(strings, flags);
                    return;
                }
            }

            foreach (KeyValuePair<teResourceGUID, UXDisplayText> str in strings) {
                Log($"{str.Key}: {str.Value.Value}");
            }
        }

        public Dictionary<teResourceGUID, UXDisplayText> GetStrings() {
            var strings = new Dictionary<teResourceGUID, UXDisplayText>();

            foreach (teResourceGUID guid in TrackedFiles[0x7C].OrderBy(teResourceGUID.Index)) {
                UXDisplayText displayText = new UXDisplayText(guid);
                if (displayText.Value == null) continue;
                strings[guid] = displayText;
            }

            return strings;
        }
    }
}
