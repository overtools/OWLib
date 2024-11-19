using System.Collections.Generic;
using System.Linq;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels.GameModes {
    public class GameRulesetGameMode {
        public teResourceGUID GUID { get; set; }
        public string Description { get; set; }
        public GameMode GameMode { get; set; }
        public List<GameRulesetTeam> Teams { get; set; }
        public GamemodeRulesetValue[] ConfigValues { get; set; }

        internal STUGameRulesetGameMode STU { get; set; }

        public GameRulesetGameMode(ulong key) {
            var stu = GetInstance<STUGameRulesetGameMode>(key);
            Init(stu, key);
        }

        public GameRulesetGameMode(STUGameRulesetGameMode stu, ulong key = default) {
            Init(stu, key);
        }

        private void Init(STUGameRulesetGameMode ruleset, ulong key = default) {
            if (ruleset == null) return;

            GUID = (teResourceGUID) key;
            Description = GetString(ruleset.m_description);
            GameMode = new GameMode(ruleset.m_gameMode);
            STU = ruleset;

            if (ruleset.m_teams != null)
                Teams = ruleset.m_teams.Select(x => new GameRulesetTeam(x)).ToList();
        }

        public class GamemodeRulesetValue {
            public teResourceGUID Virtual01C { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}
