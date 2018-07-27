using System;
using DataTool.Flag;
using TankLib.STU.Types;
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
                var data = GetInstanceNew<STU_73AE9738>(key);
                //var teamColors = GetInstanceNew<STUTeamColor>(data.m_B8DC6D46);
                var location = GetString(data.m_4BA3B3CE);
                var name = GetString(data.m_137210AF);
                var fullName = $"{location} {(string.Equals(location, name) ? "" : name)}".Trim();
                
                Log($"{fullName} ({GetString(data.m_0945E50A)})");
                Log($"\t  Division: {data.m_AA53A680}");
                
                // todo: fix later or something
                // tbh i don't think anyone is that interested
                //Log($"\t     Color: {teamColors/.Hex()}");
                //Log($"\t    Colors:");
                //Log($"\t\tColor 1: {teamColors.m_D74D5F6F.Color1?.Hex() ?? "N/A"}");
                //Log($"\t\tColor 2: {teamColors.m_D74D5F6F.Color2?.Hex() ?? "N/A"}");
                //Log($"\t\tColor 3: {teamColors.m_D74D5F6F.Color3?.Hex() ?? "N/A"}");
                //Log($"\t\tColor 4: {teamColors.m_D74D5F6F.Color4?.Hex() ?? "N/A"}");
                //Log();
            }
        }
    }
}