using System.Runtime.Serialization;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using Utf8Json;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
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

        public Loadout(ulong key) {
            STULoadout loadout = STUHelper.GetInstance<STULoadout>(key);
            if (loadout == null) return;
            Init(loadout);
        }

        public Loadout(STULoadout loadout) {
            Init(loadout);
        }

        private void Init(STULoadout loadout) {
            MovieGUID = loadout.m_infoMovie;
            
            Category = loadout.m_category;
            
            Name = GetString(loadout.m_name);
            Description = GetString(loadout.m_description);
        }
    }
}