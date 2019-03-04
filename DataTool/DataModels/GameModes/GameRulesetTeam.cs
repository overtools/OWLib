using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

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
