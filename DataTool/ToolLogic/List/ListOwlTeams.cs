using System;
using System.Collections.Generic;
using DataTool.Flag;
using DataTool.JSON;
using Newtonsoft.Json;
using OWLib;
using STULib.Types;
using STULib.Types.Dump;
using STULib.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-owl-teams", Description = "List OWL teams", TrackTypes = new ushort[] {0xEC}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListOwlTeams : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public class OwlTeam {
            public string Location;
            public string Name;
            public string Abbreviation;
            public STUEnumLeagueDivision Division;
            
            [JsonIgnore]
            public STULeagueTeam TeamData;
            
            [JsonConverter(typeof(GUIDConverter))]
            public ulong GUID;

            public OwlTeam(ulong key, string name, string location, string abbreviation, STUEnumLeagueDivision division, STULeagueTeam teamData) {
                GUID = key;
                Name = name;
                Abbreviation = abbreviation;
                Location = location;
                Division = division;
                TeamData = teamData;
            }
        }

        public void Parse(ICLIFlags toolFlags)
        {
            var owlTeams = GetOwlTeams();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(owlTeams, flags);
                    return;
                }
            
            foreach (var team in owlTeams) {
                var teamColors = GetInstance<STU_8880FCB0>(team.TeamData.Colours);
                var fullName = $"{team.Location} {(string.Equals(team.Location, team.Name) ? "" : team.Name)}".Trim();
                
                
                Log($"{fullName} ({team.Abbreviation})");
                Log($"\t  Division: {team.Division}");
                Log($"\t     Color: {teamColors.Color.Hex()}");
                Log($"\t    Colors:");
                Log($"\t\tColor 1: {teamColors.m_D74D5F6F.Color1?.Hex() ?? "N/A"}");
                Log($"\t\tColor 2: {teamColors.m_D74D5F6F.Color2?.Hex() ?? "N/A"}");
                Log($"\t\tColor 3: {teamColors.m_D74D5F6F.Color3?.Hex() ?? "N/A"}");
                Log($"\t\tColor 4: {teamColors.m_D74D5F6F.Color4?.Hex() ?? "N/A"}");
                Log();
            }
        }

        public List<OwlTeam> GetOwlTeams() {
            List<OwlTeam> @return = new List<OwlTeam>();
            
            foreach (ulong key in TrackedFiles[0xEC]) {
                var data = GetInstance<STULeagueTeam>(key);
                var location = GetString(data.Location);
                var name = GetString(data.Name);
                var abbreviation = GetString(data.Abbreviation);
                
                @return.Add(new OwlTeam(key, name, location, abbreviation, data.Division, data));
            }

            return @return;
        }
    }
}