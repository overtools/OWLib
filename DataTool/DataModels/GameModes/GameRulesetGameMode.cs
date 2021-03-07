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

        [DataMember]
        public List<GameRulesetTeam> Teams;

        [DataMember]
        public GamemodeRulesetValue[] ConfigValues;

        internal STUGameRulesetGameMode STU;

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
            STU = ruleset;

            if (ruleset.m_teams != null)
                Teams = ruleset.m_teams.Select(x => new GameRulesetTeam(x)).ToList();
        }

        public class GamemodeRulesetValue {
            [DataMember]
            public teResourceGUID Virtual01C;

            [DataMember]
            public string Name;

            [DataMember]
            public string Value;
        }
    }
}
