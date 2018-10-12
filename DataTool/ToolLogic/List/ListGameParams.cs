/*using System;
using DataTool.Flag;
using DataTool.Helper;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-game-params", Description = "List game paramaters", IsSensitive = true, CustomFlags = typeof(ListFlags))]
    public class ListGameParams : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            var iD = new IndentHelper();
            Log("Gamemode Paramaters:");

            var i = 0;
            foreach (var key in TrackedFiles[0xC6]) {
                var parameter = GetInstance<STUGameRulesetSchema>(key);
                if (parameter == null) continue;
      
                Log($"{iD+1}[{i}]:");
                Log($"{iD+2}Type: {GetString(parameter.Name) ?? "N/A"}");
                Log($"{iD+2}Params:");

                i++;
                if (parameter.GameParams == null) continue;

                var j = 0;
                foreach (var param in parameter.GameParams) {
                    Log($"{iD+3}[{j}]:");
                    Log($"{iD+4}Name: {GetString(param.Name) ?? "N/A"}");
                    Log($"{iD+4}Name2: {GetString(param.Name2) ?? "N/A"}");
                    Log($"{iD+4}01C: {param.Virtual01C.ToString()}");
                    Log($"{iD+4}Unknown: {param.UnknownInt}");
                    Log($"{iD+4}Options:");
                    j++;

                    if (param.Options != null) {
                        var jD = iD + 5;
                        switch (param.Options) {
                            case STUGameParamSwitch p:
                                Log($"{jD}Type: {p.GetType().FullName}");
                                Log($"{jD}Val1: {GetString(p.Value1)}");
                                Log($"{jD}Val2: {GetString(p.Value2)}");
                                Log($"{jD}Unkn: {p.Unknown}");
                                break;
                            case STUGameParamSlider p1:
                                Log($"{jD}Type: {p1.GetType().FullName}");
                                Log($"{jD}String: {GetString(p1.String)}");
                                Log($"{jD}Min: {p1.Min}");
                                Log($"{jD}Max: {p1.Max}");
                                Log($"{jD}Default: {p1.Default}");
                                Log($"{jD}Unknown: {p1.Unknown}");
                                break;
                            case STUGameParamDropdown p2:
                                Log($"{jD}Type: {p2.GetType().FullName}");
                                Log($"{jD}Dropdown:");
                                foreach (var droption in p2.DropdownOptions)
                                    Log($"{jD+1}- {GetString(droption.Name)}");
                                break;
                            case STUGameParamStepSlider p3:
                                Log($"{jD}Type: {p3.GetType().FullName}");
                                Log($"{jD}String: {GetString(p3.String)}");
                                Log($"{jD}Min: {p3.Min}");
                                Log($"{jD}Max: {p3.Max}");
                                Log($"{jD}Default: {p3.Default}");
                                break;
                        }
                    }
                }
            }
        }
    }
}*/