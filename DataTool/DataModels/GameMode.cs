using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    [DataContract]
    public class GameMode {
        [DataMember]
        public string DisplayName;
        
        [DataMember]
        public Enum_1964FED7 Type;
        
        [DataMember]
        public teResourceGUID[] GameRulesetSchemas;

        [DataMember]
        public teResourceGUID VoiceSet;
        
        public GameMode(STUGameMode gameMode) {
            DisplayName = GetString(gameMode.m_displayName);

            GameRulesetSchemas = Helper.JSON.FixArray(gameMode.m_gameRulesetSchemas);

            VoiceSet = gameMode.m_7F5B54B2;
            Type = gameMode.m_gameModeType;
        }
    }
}