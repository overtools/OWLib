using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.DataModels.Hero {
    [DataContract]
    public class SkinTheme {
        [DataMember]
        public teResourceGUID GUID;
        
        [DataMember]
        public teResourceGUID Skin;
        
        [DataMember]
        public teResourceGUID[] HeroWeapons;

        public SkinTheme(STU_63172E83 skinTheme) {
            GUID = skinTheme.m_5E9665E3;
            Skin = skinTheme.m_0029461B;

            HeroWeapons = Helper.JSON.FixArray(skinTheme.m_heroWeapons);
        }
    }
}