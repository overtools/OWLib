using System.Collections.Generic;
using DataTool.JSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TankLib.Math;
using TankLib.STU;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels {
    [JsonObject(MemberSerialization.OptOut)]
    public class Hero {
        [JsonConverter(typeof(GUIDConverter))]
        public ulong GUID;
        
        public string Name;
        public string Description;

        [JsonConverter(typeof(StringEnumConverter))]
        public Enum_0C014B4A Gender;
        
        public teColorRGBA GalleryColor;

        public List<Loadout> Loadouts;
        public List<HeroSkinTheme> SkinThemes;

        public Hero(ulong guid, STUHero hero) {
            GUID = guid;
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
                foreach (ulong loadout in hero.m_heroLoadout) {
                    STULoadout stuLoadout = GetInstance<STULoadout>(loadout);
                    if (stuLoadout == null) continue;
                    
                    Loadouts.Add(new Loadout(loadout, stuLoadout));
                }
            }
        }
    }

    public class HeroSkinTheme {
        [JsonConverter(typeof(GUIDConverter))]
        public ulong SkinTheme;
        [JsonConverter(typeof(GUIDConverter))]
        public ulong Skin;
        
        public List<ulong> HeroWeapons;

        public HeroSkinTheme(STU_63172E83 skinTheme) {
            SkinTheme = skinTheme.m_5E9665E3;
            Skin = skinTheme.m_0029461B;

            if (skinTheme.m_heroWeapons == null) return;
            HeroWeapons = new List<ulong>();
            foreach (teStructuredDataAssetRef<STUHeroWeapon> heroWeapon in skinTheme.m_heroWeapons) {
                HeroWeapons.Add(heroWeapon);
            }
        }
    }
}