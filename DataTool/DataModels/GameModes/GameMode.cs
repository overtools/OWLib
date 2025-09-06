#nullable enable
using System.Linq;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels.GameModes;

public class GameMode {
    public teResourceGUID GUID { get; set; }
    public string? Name { get; set; }
    public Enum_1964FED7 Type { get; set; }
    public teResourceGUID[] GameRulesetSchemas { get; set; }
    public teResourceGUID VoiceSet { get; set; }

    public GameMode(STUGameMode stu, ulong key = default) {
        Init(stu, key);
    }

    private void Init(STUGameMode? gamemode, ulong key = default) {
        if (gamemode == null) return;

        GUID = (teResourceGUID) key;
        Name = GetString(gamemode.m_displayName);
        GameRulesetSchemas = gamemode.m_gameRulesetSchemas?.Select(x => x.GUID).ToArray();
        VoiceSet = gamemode.m_7F5B54B2;
        Type = gamemode.m_gameModeType;
    }

    public GameModeLite ToLite() {
        return new GameModeLite(this);
    }

    public static GameMode? Load(ulong guid) {
        var stu = GetInstance<STUGameMode>(guid);
        if (stu == null) return null;
        return new GameMode(stu, guid);
    }
}

public class GameModeLite {
    public teResourceGUID GUID { get; set; }
    public string Name { get; set; }

    public GameModeLite(GameMode gameMode) {
        GUID = gameMode.GUID;
        Name = gameMode.Name;
    }
}