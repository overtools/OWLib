using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels.GameModes {
    [DataContract]
    public class GameRulesetGameMode {
        [DataMember]
        public teResourceGUID GUID;

        [DataMember]
        public string Description;

        [DataMember]
        public GameMode GameMode;

        public List<GameRulesetTeam> Teams;
        
        public GameRulesetGameMode(ulong key) {
            STUGameRulesetGameMode stu = GetInstance<STUGameRulesetGameMode>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public GameRulesetGameMode(STUGameRulesetGameMode stu) {
            Init(stu);
        }

        private void Init(STUGameRulesetGameMode ruleset, ulong key = default) {
            GUID = (teResourceGUID) key;
            Description = GetString(ruleset.m_description);
            GameMode = new GameMode(ruleset.m_gamemode);

            if (ruleset.m_teams != null)
                Teams = ruleset.m_teams.Select(x => new GameRulesetTeam(x)).ToList();
        }
    }
}
