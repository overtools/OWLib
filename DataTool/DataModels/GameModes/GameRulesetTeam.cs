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
        public TeamIndexFuckYou Team;

        [DataMember]
        public int MaxPlayers;

        public GameRulesetTeam(STUGameRulesetTeam team) {
            MaxPlayers = team.m_341EF5FA;


            switch (team.m_team) {
                case STU_00C5C6F0 t1:
                    Team = (TeamIndexFuckYou) t1.m_team;
                    break;
            }
        }
    }
}
