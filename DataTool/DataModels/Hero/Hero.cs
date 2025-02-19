using System.Collections.Generic;
using System.Diagnostics;
using TankLib;
using TankLib.Math;
using TankLib.STU;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels.Hero {
    [DebuggerDisplay("[{GUID.ToStringShort()}] {Name}")]
    public class Hero {
        public teResourceGUID GUID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Class { get; set; }
        public Enum_0C014B4A Gender { get; set; }
        public STUHeroSize Size { get; set; }
        public string Color { get; set; }
        public string sRGBColor { get; set; }
        public teColorRGBA GalleryColor { get; set; }
        public bool IsHero { get; set; }
        public bool SupportsAi { get; set; }
        public List<LoadoutLite> Loadouts { get; set; }
        public List<LoadoutLite> Perks { get; set; }
        public List<HeroImage> Images { get; set; }

        internal STUHero STU { get; set; }

        public Hero(ulong key) {
            STUHero stu = GetInstance<STUHero>(key);
            Init(stu, key);
        }

        public Hero(STUHero stu, ulong key = default) {
            Init(stu, key);
        }

        private void Init(STUHero hero, ulong key = default) {
            if (hero == null) return;

            STU = hero;
            GUID = (teResourceGUID) key;
            Name = GetCleanName(hero);
            Description = GetDescriptionString(hero.m_3446F580);
            Class = GetNullableGUIDName(hero.m_category);
            Gender = hero.m_gender;
            Size = hero.m_heroSize;
            GalleryColor = hero.m_heroColor;
            Color = GalleryColor.ToHex();
            sRGBColor = GalleryColor.ToNonLinear().ToHex();
            SupportsAi = hero.m_906C3711 > 0;
            IsHero = hero.m_64DC571F > 0;

            
            if (hero.m_heroLoadout != null) {
                Loadouts = new List<LoadoutLite>();
                foreach (var loadoutGUID in hero.m_heroLoadout) {
                    var loadout = new Loadout(loadoutGUID);
                    if (loadout.GUID == 0) continue;
                    Loadouts.Add(loadout.ToLite());
                }
            }

            if (hero.m_B25192D9 != null) {
                Perks = new List<LoadoutLite>();
                foreach (teStructuredDataAssetRef<STU_42B75C40> perk in hero.m_B25192D9) {
                    var loadout = new Loadout(GetInstance<STU_42B75C40>(perk).m_loadout);
                    if (loadout.GUID == 0) continue;
                    Perks.Add(loadout.ToLite());
                }
            }

            // Contains array of various hero images, hero gallery portraits, small hero select icons, etc.
            if (hero.m_8203BFE1 != null) {
                Images = new List<HeroImage>();
                foreach (var imageSet in hero.m_8203BFE1) {
                    Images.Add(new HeroImage {
                        Id = imageSet.m_id,
                        TextureGUID = imageSet.m_texture
                    });
                }
            }
        }

        public ProgressionUnlocks GetUnlocks() {
            return new ProgressionUnlocks(STU);
        }

        public static string GetName(ulong key) {
            var stu = GetInstance<STUHero>(key);
            if (stu == null) return null;

            return GetCleanName(stu);
        }

        public static string GetCleanName(STUHero hero) {
            return GetCleanString(hero.m_0EDCE350);
        }

        public class HeroImage {
            public teResourceGUID Id { get; set; }
            public teResourceGUID TextureGUID { get; set; }
        }
    }
}