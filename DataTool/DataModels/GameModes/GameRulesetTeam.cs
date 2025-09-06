using System.Linq;
using TankLib.STU.Types;

namespace DataTool.DataModels.GameModes;

public enum TeamIndexFuckYou {
    Defending = 0x0, // TeamBlue
    Attacking = 0x1, // TeamRed
    FFA = 0x4
}

public class GameRulesetTeam {
    public string Team { get; set; }
    public int MaxPlayers { get; set; }
    public string[] AvailableHeroes { get; set; }
    public GameRulesetGameMode.GamemodeRulesetValue[] ConfigValues { get; set; }

    internal STUGameRulesetTeam STU { get; set; }

    public GameRulesetTeam(STUGameRulesetTeam team) {
        MaxPlayers = team.m_341EF5FA;
        STU = team;

        switch (team.m_availableHeroes) {
            case STU_C45DE560 stu:
                AvailableHeroes = stu.m_heroes?.Select(x => Hero.Hero.GetName(x)).ToArray();
                break;
        }

        switch (team.m_team) {
            case STU_00C5C6F0 stu:
                Team = ((TeamIndexFuckYou) stu.m_team).ToString();
                break;
            case STU_97DAE7E1 stu:
                Team = "Unknown";
                break;
        }
    }
}