using System;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using static DataTool.Program;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.List {
    [Tool("list-owl-teams", Description = "List OWL teams", TrackTypes = new ushort[] {0xEC}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListOwlTeams : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {;
            var teams = TrackedFiles[0xEC].Select(key => new TeamDefinition(key));
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(teams, flags);
                    return;
                }
            
            foreach (var team in teams) {
                Log($"{team.FullName}");
                Log($"\tDivision: {team.Division}");
                
                if (!string.IsNullOrEmpty(team.Abbreviation))
                    Log($"\tAbbreviation: {team.Abbreviation}");
                
                Log();

                // todo: fix later or something
                // tbh i don't think anyone is that interested
                //Log($"\t     Color: {teamColors/.Hex()}");
                //Log($"\t    Colors:");
                //Log($"\t\tColor 1: {teamColors.m_D74D5F6F.Color1?.Hex() ?? "N/A"}");
                //Log($"\t\tColor 2: {teamColors.m_D74D5F6F.Color2?.Hex() ?? "N/A"}");
                //Log($"\t\tColor 3: {teamColors.m_D74D5F6F.Color3?.Hex() ?? "N/A"}");
                //Log($"\t\tColor 4: {teamColors.m_D74D5F6F.Color4?.Hex() ?? "N/A"}");
            }
        }
    }
}