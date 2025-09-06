﻿using System.Linq;
using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.JSON;
using static DataTool.Program;

namespace DataTool.ToolLogic.List.Misc;

[Tool("list-esport-teams", Description = "Lists eSport teams", CustomFlags = typeof(ListFlags), IsSensitive = true)]
public class ListEsportTeams : JSONTool, ITool {
    public void Parse(ICLIFlags toolFlags) {
        var flags = (ListFlags) toolFlags;
        var teams = GetTeams();

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
        return TrackedFiles[0xEC].Select(TeamDefinition.Load);
    }
}