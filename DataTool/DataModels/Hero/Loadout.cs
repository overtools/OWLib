using System.Runtime.Serialization;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels.Hero {
    [DataContract]
    public class Loadout {
        [DataMember]
        public string Name;
        
        [DataMember]
        public string Description;
        
        [DataMember]
        public LoadoutCategory Category;
        
        [DataMember]
        public teResourceGUID MovieGUID;

        public Loadout(STULoadout loadout) {
            MovieGUID = loadout.m_infoMovie;
            
            Category = loadout.m_category;
            
            Name = GetString(loadout.m_name);
            Description = GetString(loadout.m_description);
        }

        public static Loadout GetLoadout(ulong key) {
            STULoadout loadout = STUHelper.GetInstance<STULoadout>(key);
            if (loadout == null) return null;
            return new Loadout(loadout);
        }
    }
}