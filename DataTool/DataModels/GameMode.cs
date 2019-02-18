using System.Collections.Generic;
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
        public string InternalName;
        
        [DataMember]
        public Enum_1964FED7 Type;
        
        [DataMember]
        public teResourceGUID[] GameRulesetSchemas;

        [DataMember]
        public teResourceGUID VoiceSet;
        
        public GameMode(STUGameMode gameMode, ulong key) {
            DisplayName = GetString(gameMode.m_displayName);
            InternalName = GetInternalName(key);

            GameRulesetSchemas = Helper.JSON.FixArray(gameMode.m_gameRulesetSchemas);

            VoiceSet = gameMode.m_7F5B54B2;
            Type = gameMode.m_gameModeType;
        }
        
        private string GetInternalName(ulong key) {
            InternalGamemodeNames.TryGetValue(key, out string gamemode);
            return gamemode ?? $"UNKNOWN {teResourceGUID.AsString(key)}";
        }

        private static readonly Dictionary<ulong, string> InternalGamemodeNames = new Dictionary<ulong, string> {
            {0x023000000000000F, "Omnic Flashback"},
            {0x023000000000001A, "Omnic Flashback All Heroes"},
            {0x023000000000001D, "Team Deathmatch"},
            {0x023000000000001E, "Deathmatch"},
            {0x023000000000002A, "Halloween Holdout Endless"},
            {0x023000000000004A, "Survivor"},
            {0x0230000000000003, "Halloween Holdout"},
            {0x0230000000000007, "CTF"},
            {0x0230000000000008, "Winter Offensive"},
            {0x0230000000000009, "Elimination"},
            {0x0230000000000010, "Skirmish"},
            {0x0230000000000014, "Assault"},
            {0x0230000000000015, "Payload"},
            {0x0230000000000016, "Hybrid"},
            {0x0230000000000017, "Control"},
            {0x0230000000000018, "Practice Range"},
            {0x0230000000000019, "Tutorial"},
            {0x0230000000000020, "Soccer"},
            {0x0230000000000025, "Retribution"},
            {0x0230000000000029, "Yeti Hunter"}
        };
    }
}