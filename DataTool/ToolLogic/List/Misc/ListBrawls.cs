using System.Collections.Generic;
using DataTool.DataModels.GameModes;
using DataTool.Flag;
using DataTool.JSON;
using static DataTool.Program;

namespace DataTool.ToolLogic.List.Misc;

[Tool("list-brawls", Description = "List brawls", CustomFlags = typeof(ListFlags), IsSensitive = true)]
public class ListBrawls : JSONTool, ITool {
    public void Parse(ICLIFlags toolFlags) {
        var flags = (ListFlags) toolFlags;
        var data = GetData();
        OutputJSON(data, flags);
    }

    private static List<Brawl> GetData() {
        var brawls = new List<Brawl>();

        foreach (ulong key in TrackedFiles[0xC7]) {
            var brawl = Brawl.Load(key);
            if (brawl == null) continue;
            brawls.Add(brawl);
        }

        return brawls;
    }
}