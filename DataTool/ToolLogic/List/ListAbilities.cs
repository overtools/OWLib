using System.Collections.Generic;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;

namespace DataTool.ToolLogic.List {
    [Tool("list-abilities", Description = "List abilities", CustomFlags = typeof(ListFlags))]
    public class ListAbilities : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ListFlags) toolFlags;
            var data = GetData();

            if (flags.JSON) {
                OutputJSON(data, flags);
                return;
            }

            IndentHelper indentLevel = new IndentHelper();
            foreach (var loadout in data) {
                Log($"{indentLevel}{loadout.Value.Name}:");
                if (!flags.Simplify) {
                    Log($"{indentLevel + 1}       Type: {loadout.Value.Category}");
                    Log($"{indentLevel + 1}     Button: {loadout.Value.Button ?? "None"}");
                    Log($"{indentLevel + 1}Description: {loadout.Value.Description}");
                    Log();
                }
            }
        }

        private Dictionary<teResourceGUID, Loadout> GetData() {
            var @return = new Dictionary<teResourceGUID, Loadout>();

            foreach (teResourceGUID key in TrackedFiles[0x9E]) {
                var loadout = Loadout.Load(key);
                if (loadout == null) continue;
                @return.Add(key, loadout);
            }

            return @return;
        }
    }
}
