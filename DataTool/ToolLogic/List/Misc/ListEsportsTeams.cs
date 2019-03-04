using System.Linq;
using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.JSON;
using static DataTool.Program;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-esport-teams", Description = "Lists eSport teams", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListEsportTeams : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {;
            var teams = GetTeams();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(teams, flags);
                    return;
                }
            
            foreach (var team in teams) {
                Log($"{team.FullName}");
                Log($"\tDivision: {team.Division}");
                
                if (!string.IsNullOrEmpty(team.Abbreviation))
                    Log($"\tAbbreviation: {team.Abbreviation}");
                
                Log();
            }
        }

        public IEnumerable<TeamDefinition> GetTeams() {
            return TrackedFiles[0xEC].Select(key => new TeamDefinition(key));
        }
    }
}