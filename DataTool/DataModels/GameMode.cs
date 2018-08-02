using DataTool.JSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    [JsonObject(MemberSerialization.OptOut)]
    public class GameMode {
        public string DisplayName;
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Enum_1964FED7 Type;
        
        [JsonConverter(typeof(GUIDArrayConverter))]
        public teResourceGUID[] GameRulesetSchemas;

        [JsonConverter(typeof(GUIDConverter))]
        public teResourceGUID VoiceSet;
        
        public GameMode(STUGameMode gameMode) {
            DisplayName = GetString(gameMode.m_displayName);

            if (gameMode.m_gameRulesetSchemas != null) {
                GameRulesetSchemas = new teResourceGUID[gameMode.m_gameRulesetSchemas.Length];

                for (int i = 0; i < GameRulesetSchemas.Length; i++) {
                    GameRulesetSchemas[i] = gameMode.m_gameRulesetSchemas[i];
                }
            }

            VoiceSet = gameMode.m_7F5B54B2;
            Type = gameMode.m_gameModeType;
        }
    }
}