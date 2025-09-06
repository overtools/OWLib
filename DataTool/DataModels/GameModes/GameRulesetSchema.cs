#nullable enable
using System.Linq;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.IO;

namespace DataTool.DataModels.GameModes;

public class GameRulesetSchema {
    public teResourceGUID GUID { get; set; }
    public string? Name { get; set; }
    public GameRulesetSchemaEntry[]? Entries { get; set; }

    public GameRulesetSchema(STUGameRulesetSchema? stu, ulong key = default) {
        Init(stu, key);
    }

    private void Init(STUGameRulesetSchema? ruleset, ulong key = default) {
        if (ruleset == null) return;

        GUID = (teResourceGUID) key;
        Name = GetString(ruleset.m_displayText);
        Entries = ruleset.m_entries?.Select(x => new GameRulesetSchemaEntry(x)).ToArray();
    }

    public static GameRulesetSchema? Load(ulong key) {
        var stu = STUHelper.GetInstance<STUGameRulesetSchema>(key);
        if (stu == null) return null;
        return new GameRulesetSchema(stu, key);
    }
}