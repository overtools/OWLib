using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels {
    [JsonObject(MemberSerialization.OptOut)]
    public class TeamDefinition {
        public string Name;
        public string Location;
        public string Abbreviation;
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Enum_5A789F71 Division;

        public string FullName;
        
        public TeamDefinition(STU_73AE9738 def) {
            Init(def);
        }

        public TeamDefinition(ulong guid) {
            var def = GetInstance<STU_73AE9738>(guid);
            Init(def);
        }

        private void Init(STU_73AE9738 def) {
            if (def == null) return;
            
            Name = GetString(def.m_137210AF);
            Location = GetString(def.m_4BA3B3CE);
            Abbreviation = GetString(def.m_0945E50A);
            
            Division = def.m_AA53A680;
            
            FullName = $"{Location} {(string.Equals(Location, Name) ? "" : Name)}".Trim();
        }
    }
}