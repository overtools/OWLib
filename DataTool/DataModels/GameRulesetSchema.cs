using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TankLib.STU.Types;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    [DataContract]
    public class GameRulesetSchema {
        [DataMember]
        public string Name;

        [DataMember]
        public IEnumerable<GameRulesetSchemaEntry> Entries;
        
        public GameRulesetSchema(STUGameRulesetSchema ruleset) {
            Name = GetString(ruleset.m_displayText);
            Entries = ruleset.m_entries != null ? ruleset.m_entries.Select(x => new GameRulesetSchemaEntry(x)) : Enumerable.Empty<GameRulesetSchemaEntry>();
        }
    }
}