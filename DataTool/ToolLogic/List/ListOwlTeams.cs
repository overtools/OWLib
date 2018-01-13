using System;
using DataTool.Flag;
using STULib.Types;
using STULib.Types.OWL;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-owl-teams", Description = "List OWL teams", TrackTypes = new ushort[] {0xEC}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListShit : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            foreach (ulong key in TrackedFiles[0xEC]) {
                var data = GetInstance<STULeagueTeam>(key);
                var teamColors = GetInstance<STU_8880FCB0>(data.Colours);
                
                Log($"{GetString(data.Location)} {GetString(data.Name)} ({ GetString(data.Abbreviation)})");
                Log($"\t Division: #{data.Division}");
                Log($"\t    Color: #{teamColors.Color.Hex().Substring(3)}");
                Log("\tm_D74D5F6F:");
                Log($"\t\tColor 1: #{teamColors.m_D74D5F6F.Color1.Hex().Substring(3)}");
                Log($"\t\tColor 2: #{teamColors.m_D74D5F6F.Color2.Hex().Substring(3)}");
                Log($"\t\tColor 3: #{teamColors.m_D74D5F6F.Color3?.Hex().Substring(3)}"); // Sometimes is null
                Log($"\t\tColor 4: #{teamColors.m_D74D5F6F.Color4.Hex().Substring(3)}");
            }
        }
    }
}