using System.Linq;
using System.Runtime.Serialization;
using TankLib.STU.Types;

namespace DataTool.DataModels.GameModes {
    public enum TeamIndexFuckYou {
        Defending = 0x0, // TeamBlue
        Attacking = 0x1, // TeamRed
        FFA = 0x4
    }

    [DataContract]
    public class GameRulesetTeam {
        [DataMember]
        public string Team;

        [DataMember]
        public int MaxPlayers;

        [DataMember]
        public string[] AvailableHeroes;

        [DataMember]
        public GameRulesetGameMode.GamemodeRulesetValue[] ConfigValues;

        internal STUGameRulesetTeam STU;

        public GameRulesetTeam(STUGameRulesetTeam team) {
            MaxPlayers = team.m_341EF5FA;
            STU = team;

            switch (team.m_availableHeroes) {
                case STU_C45DE560 stu:
                    AvailableHeroes = stu.m_heroes?.Select(x => new Hero.Hero(x).Name).ToArray();
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
}
