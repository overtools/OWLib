using System.Collections.Generic;
using System.Runtime.Serialization;
using TankLib;
using TankLib.Math;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels.Hero {
    [DataContract]
    public class Hero {
        [DataMember]
        public teResourceGUID GUID;
        
        [DataMember]
        public string Name;
        
        [DataMember]
        public string Description;

        [DataMember]
        public Enum_0C014B4A Gender;

        [DataMember]
        public Enum_C1DAF32A Size;
        
        [DataMember]
        public byte IsNpc;
        
        [DataMember]
        public byte SupportsAi;
        
        [DataMember]
        public teColorRGBA GalleryColor;

        [DataMember]
        public List<Loadout> Loadouts;
        
        [DataMember]
        public List<SkinTheme> SkinThemes;
        
        public Hero(ulong key) {
            STUHero stu = GetInstance<STUHero>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public Hero(STUHero stu) {
            Init(stu);
        }

        private void Init(STUHero hero, ulong key = default) {
            GUID = (teResourceGUID) key;
            Name = GetString(hero.m_0EDCE350);
            Description = GetDescriptionString(hero.m_3446F580);
            Gender = hero.m_gender;
            Size = hero.m_heroSize;
            
            GalleryColor = hero.m_heroColor;
            SupportsAi = hero.m_906C3711;
            IsNpc = hero.m_62746D34;

            //if (hero.m_skinThemes != null) {
            //    SkinThemes = new List<HeroSkinTheme>();
            //    foreach (STU_63172E83 skinTheme in hero.m_skinThemes) {
            //        SkinThemes.Add(new HeroSkinTheme(skinTheme));
            //    }
            //}
            
            if (hero.m_heroLoadout != null) {
                Loadouts = new List<Loadout>();
                foreach (teResourceGUID loadoutGUID in hero.m_heroLoadout) {
                    var loadout = Loadout.GetLoadout(loadoutGUID);
                    if (loadout == null) continue;
                    Loadouts.Add(loadout);
                }
            }
        }
    }
}