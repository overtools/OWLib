using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.JSON;
using static DataTool.Program;

namespace DataTool.ToolLogic.List {
    [Tool("list-all-unlocks", Description = "List hero unlocks", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListAllUnlocks : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = TrackedFiles[0xA5].Select(key => new Unlock(key)).Where(unlock => unlock.GUID != 0).ToList();

            if (toolFlags is ListFlags flags) {
                OutputJSON(data, flags);
            }
        }
    }
}
