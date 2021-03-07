using System.Collections.Generic;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.List {
    [Tool("list-abilities", Description = "List abilities", CustomFlags = typeof(ListFlags))]
    public class ListAbilities : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(data, flags);
                    return;
                }

            IndentHelper indentLevel = new IndentHelper();
            foreach (var loadout in data) {
                Log($"{indentLevel}{loadout.Value.Name}:");
                if (!(toolFlags as ListFlags).Simplify) {
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
                var loadout = new Loadout(key);
                if (loadout.GUID == 0) continue;
                @return.Add(key, loadout);
            }

            return @return;
        }
    }
}
