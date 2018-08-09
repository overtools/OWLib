using System;
using DataTool.DataModels;
using DataTool.Flag;
using static DataTool.Program;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.List {
    [Tool("list-owl-teams", Description = "List OWL teams", TrackTypes = new ushort[] {0xEC}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListOwlTeams : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            foreach (ulong key in TrackedFiles[0xEC]) {
                TeamDefinition teamDef = new TeamDefinition(key);
                
                Log($"{teamDef.FullName} ({teamDef.Abbreviation})");
                Log($"    Division: {teamDef.Division}");
                

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