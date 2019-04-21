using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.JSON;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-ranked-seasons", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListRankedSeasons : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(data, flags);
                    return;
                }
        }

        private static List<RankedSeason> GetData() {
            var seasons = new List<RankedSeason>();
            
            foreach (ulong key in TrackedFiles[0xC8]) {
                var season = new RankedSeason(key);
                if (season.GUID != 0)
                    seasons.Add(season);
            }

            return seasons;
        }
    }
}