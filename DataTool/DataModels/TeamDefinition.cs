using DataTool.JSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels {
    [JsonObject(MemberSerialization.OptOut)]
    public class TeamDefinition {
        public string FullName;
        public string Name;
        public string Location;
        public string Abbreviation;
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Enum_5A789F71 Division;
        
        [JsonConverter(typeof(GUIDConverter))]
        public ulong Logo;
        
        [JsonConverter(typeof(GUIDConverter))]
        public ulong LogoAlt;
        
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
            Logo = def.m_AC77C84A;
            LogoAlt = def.m_DA688288;
            
            Division = def.m_AA53A680;
            
            FullName = $"{Location} {(string.Equals(Location, Name) ? "" : Name)}".Trim();
        }
    }
}