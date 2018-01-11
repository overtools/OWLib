using System;
using DataTool.Flag;
using STULib.Types;
using STULib.Types.ZeroCE;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-owl-teams", Description = "List owl teams", TrackTypes = new ushort[] {0x0EC}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListShit : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            foreach (ulong key in TrackedFiles[0x0EC]) {
                var data = GetInstance<STULeagueTeam>(key);
                var TeamCity = GetString(data.TeamCity);
                var TeamName = GetString(data.TeamName);
                var TeamCode = GetString(data.TeamCode);
                
                var TeamInfo = GetInstance<STU_8880FCB0>(data.m_B8DC6D46);
                var color = TeamInfo.Color.Hex().Substring(3);
                
                Log($"{TeamCity} {TeamName} ({TeamCode})");
                Log($"\t Division: #{data.Division}");
                Log($"\t    Color: #{color}");
                Log("\tm_D74D5F6F:");
                Log($"\t\tColor 1: #{TeamInfo.m_D74D5F6F.Color1.Hex().Substring(3)}");
                Log($"\t\tColor 2: #{TeamInfo.m_D74D5F6F.Color2.Hex().Substring(3)}");
                Log($"\t\tColor 3: #{TeamInfo.m_D74D5F6F.Color3?.Hex().Substring(3)}"); // Sometimes is null
                Log($"\t\tColor 4: #{TeamInfo.m_D74D5F6F.Color4.Hex().Substring(3)}");
            }
        }
    }
}