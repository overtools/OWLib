using System.Collections.Generic;
using DataTool.DataModels.GameModes;
using DataTool.Flag;
using DataTool.JSON;
using static DataTool.Program;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-brawls", Description = "List brawls", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListBrawls : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(data, flags);
                    return;
                }
        }

        private static List<Brawl> GetData() {
            var brawls = new List<Brawl>();

            foreach (ulong key in TrackedFiles[0xC7]) {
                var brawl = new Brawl(key);
                if (brawl.GUID == 0) continue;
                
                brawls.Add(brawl);
            }

            return brawls;
        }
    }
}