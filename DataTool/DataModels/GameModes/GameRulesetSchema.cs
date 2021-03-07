using System.Linq;
using System.Runtime.Serialization;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.IO;

namespace DataTool.DataModels.GameModes {
    [DataContract]
    public class GameRulesetSchema {
        [DataMember]
        public string GUID;

        [DataMember]
        public string Name;

        [DataMember]
        public GameRulesetSchemaEntry[] Entries;

        public GameRulesetSchema(ulong key) {
            var stu = STUHelper.GetInstance<STUGameRulesetSchema>(key);
            Init(stu, key);
        }

        public GameRulesetSchema(STUGameRulesetSchema stu, ulong key = default) {
            Init(stu, key);
        }

        private void Init(STUGameRulesetSchema ruleset, ulong key = default) {
            GUID = teResourceGUID.AsString(key);
            Name = GetString(ruleset.m_displayText);
            Entries = ruleset.m_entries?.Select(x => new GameRulesetSchemaEntry(x)).ToArray();
        }
    }
}
