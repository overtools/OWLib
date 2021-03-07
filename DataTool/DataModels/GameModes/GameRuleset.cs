using System.Linq;
using System.Runtime.Serialization;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.DataModels.GameModes {
    public class GameRuleset {
        [DataMember]
        public teResourceGUID GUID;

        [DataMember]
        public GameRulesetGameMode GameMode;

        [DataMember]
        public string[] WorkshopRules;

        [DataMember]
        public string WorkshopScript;

        public GameRuleset(ulong key) {
            var stu = STUHelper.GetInstance<STUGameRuleset>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public GameRuleset(STUGameRuleset stu) {
            Init(stu);
        }

        private void Init(STUGameRuleset stu, ulong key = default) {
            GUID = (teResourceGUID) key;
            GameMode = stu.m_gamemode == null ? null : new GameRulesetGameMode(stu.m_gamemode);
            WorkshopRules = stu.m_26E97DBB?.Select(x => IO.GetString(x)).ToArray();
            WorkshopScript = stu.m_2690B60B;
        }
    }
}
