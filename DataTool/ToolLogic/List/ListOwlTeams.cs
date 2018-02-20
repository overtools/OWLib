using System;
using System.Diagnostics;
using DataTool.Flag;
using STULib.Types;
using STULib.Types.Dump;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-owl-teams", Description = "List OWL teams", TrackTypes = new ushort[] {0xEC}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListOwlTeams : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            foreach (ulong key in TrackedFiles[0xEC]) {
                var data = GetInstance<STULeagueTeam>(key);
                var teamColors = GetInstance<STU_8880FCB0>(data.Colours);
                var location = GetString(data.Location);
                var name = GetString(data.Name);
                var fullName = $"{location} {(string.Equals(location, name) ? "" : name)}".Trim();
                
                Log($"{fullName} ({GetString(data.Abbreviation)})");
                Log($"\t  Division: {data.Division}");
                Log($"\t     Color: {teamColors.Color.Hex()}");
                Log($"\t    Colors:");
                Log($"\t\tColor 1: {teamColors.m_D74D5F6F.Color1?.Hex() ?? "N/A"}");
                Log($"\t\tColor 2: {teamColors.m_D74D5F6F.Color2?.Hex() ?? "N/A"}");
                Log($"\t\tColor 3: {teamColors.m_D74D5F6F.Color3?.Hex() ?? "N/A"}");
                Log($"\t\tColor 4: {teamColors.m_D74D5F6F.Color4?.Hex() ?? "N/A"}");
                Log();
            }
        }
    }
}