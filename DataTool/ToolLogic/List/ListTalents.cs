using System.Collections.Generic;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;

namespace DataTool.ToolLogic.List;

[Tool("list-talents", Description = "List talents", CustomFlags = typeof(ListFlags))]
public class ListTalents : JSONTool, ITool {
    public void Parse(ICLIFlags toolFlags) {
        var flags = (ListFlags) toolFlags;
        var data = GetData();

        if (flags.JSON) {
            OutputJSON(data, flags);
            return;
        }

        IndentHelper indentLevel = new IndentHelper();
        foreach (var (key, talent) in data) {
            Log($"{indentLevel}{talent.Name}:");
            if (!flags.Simplify) {
                Log($"{indentLevel + 1}Type: {talent.TalentType}");
                Log($"{indentLevel + 1}Description: {talent.Description}");
                Log($"{indentLevel + 1}Rarity: {talent.Rarity?.Value}");
                Log($"{indentLevel + 1}Category: {talent.Category?.Value}");

                Log();
            }
        }
    }

    public static Dictionary<teResourceGUID, Talent> GetData() {
        var map = new Dictionary<teResourceGUID, Talent>();

        foreach (teResourceGUID guid in Program.TrackedFiles[0x134]) {
            var talent = Talent.Load(guid);
            if (talent == null) continue;
            map.Add(guid, talent);
        }

        return map;
    }
}