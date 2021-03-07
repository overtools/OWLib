using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels.GameModes {
    [DataContract]
    public class GameMode {
        [DataMember]
        public teResourceGUID GUID;

        [DataMember]
        public string Name;

        [DataMember]
        public Enum_1964FED7 Type;

        [DataMember]
        public teResourceGUID[] GameRulesetSchemas;

        [DataMember]
        public teResourceGUID VoiceSet;

        public GameMode(ulong key) {
            STUGameMode stu = GetInstance<STUGameMode>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public GameMode(STUGameMode stu) {
            Init(stu);
        }

        private void Init(STUGameMode gamemode, ulong key = default) {
            GUID = (teResourceGUID) key;
            Name = GetString(gamemode.m_displayName);
            GameRulesetSchemas = Helper.JSON.FixArray(gamemode.m_gameRulesetSchemas);
            VoiceSet = gamemode.m_7F5B54B2;
            Type = gamemode.m_gameModeType;
        }

        public GameModeLite ToLite() {
            return new GameModeLite(this);
        }
    }

    public class GameModeLite {
        [DataMember]
        public teResourceGUID GUID;

        [DataMember]
        public string Name;

        public GameModeLite(GameMode gameMode) {
            GUID = gameMode.GUID;
            Name = gameMode.Name;
        }
    }
}
