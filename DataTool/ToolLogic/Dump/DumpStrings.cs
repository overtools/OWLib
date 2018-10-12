using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-strings", Description = "Dump strings", CustomFlags = typeof(DumpFlags))]
    public class DumpStrings : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var strings = GetStrings();

            if (toolFlags is DumpFlags flags) {
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