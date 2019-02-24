using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels {
    [DataContract]
    public class TeamDefinition {
        [DataMember]
        public teResourceGUID Id;
        
        [DataMember]
        public string FullName;
        
        [DataMember]
        public string Name;
        
        [DataMember]
        public string Location;
        
        [DataMember]
        public string Abbreviation;
        
        [DataMember]
        public Enum_5A789F71 Division;
        
        [DataMember]
        public teResourceGUID Logo;
        
        [DataMember]
        public teResourceGUID LogoAlt;
        
        public TeamDefinition(STU_73AE9738 def) {
            Init(def);
        }

        public TeamDefinition(ulong guid) {
            var def = GetInstance<STU_73AE9738>(guid);
            Init(def, guid);
        }

        private void Init(STU_73AE9738 def, ulong key = default) {
            if (def == null) return;

            Id = (teResourceGUID) key;
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