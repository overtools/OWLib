using System.Collections.Generic;
using System.Runtime.Serialization;
using DataTool.JSON;
using TankLib;
using TankLib.Math;
using TankLib.STU;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using Utf8Json;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels {
    [DataContract]
    public class Hero {
        [DataMember]
        public string Name;
        
        [DataMember]
        public string Description;

        [DataMember]
        public Enum_0C014B4A Gender;
        
        [DataMember]
        public teColorRGBA GalleryColor;

        [DataMember]
        public List<Loadout> Loadouts;
        
        [DataMember]
        public List<HeroSkinTheme> SkinThemes;

        public Hero(STUHero hero) {
            Name = GetString(hero.m_0EDCE350);
            Description = GetDescriptionString(hero.m_3446F580);
            Gender = hero.Gender;
            
            GalleryColor = hero.m_heroColor;

            //if (hero.m_skinThemes != null) {
            //    SkinThemes = new List<HeroSkinTheme>();
            //    foreach (STU_63172E83 skinTheme in hero.m_skinThemes) {
            //        SkinThemes.Add(new HeroSkinTheme(skinTheme));
            //    }
            //}
            
            if (hero.m_heroLoadout != null) {
                Loadouts = new List<Loadout>();
                foreach (teResourceGUID loadout in hero.m_heroLoadout) {
                    STULoadout stuLoadout = GetInstance<STULoadout>(loadout);
                    if (stuLoadout == null) continue;
                    
                    Loadouts.Add(new Loadout(stuLoadout));
                }
            }
        }
    }

    [DataContract]
    public class HeroSkinTheme {
        [DataMember]
        public teResourceGUID SkinTheme;
        
        [DataMember]
        public teResourceGUID Skin;
        
        [DataMember]
        public teResourceGUID[] HeroWeapons;

        public HeroSkinTheme(STU_63172E83 skinTheme) {
            SkinTheme = skinTheme.m_5E9665E3;
            Skin = skinTheme.m_0029461B;

            HeroWeapons = Helper.JSON.FixArray(skinTheme.m_heroWeapons);
        }
    }
}