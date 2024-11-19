using System.Linq;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.DataModels.GameModes {
    public class GameRuleset {
        public teResourceGUID GUID { get; set; }
        public GameRulesetGameMode GameMode { get; set; }
        public string[] WorkshopRules { get; set; }
        public string WorkshopScript { get; set; }

        public GameRuleset(ulong key) {
            var stu = STUHelper.GetInstance<STUGameRuleset>(key);
            Init(stu, key);
        }

        public GameRuleset(STUGameRuleset stu, ulong key = default) {
            Init(stu, key);
        }

        private void Init(STUGameRuleset stu, ulong key = default) {
            if (stu == null) return;

            GUID = (teResourceGUID) key;
            GameMode = stu.m_gameMode == null ? null : new GameRulesetGameMode(stu.m_gameMode);
            WorkshopRules = stu.m_26E97DBB?.Select(x => IO.GetString(x)).ToArray();
            WorkshopScript = stu.m_2690B60B;
        }
    }
}
