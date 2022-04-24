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
        public teResourceGUID GUID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Class { get; set; }

        [DataMember]
        public Enum_0C014B4A Gender { get; set; }

        [DataMember]
        public STUHeroSize Size { get; set; }

        [DataMember]
        public string Color { get; set; }

        [DataMember]
        public string sRGBColor { get; set; }

        [DataMember]
        public teColorRGBA GalleryColor { get; set; }

        [DataMember]
        public bool IsHero { get; set; }

        [DataMember]
        public bool SupportsAi { get; set; }

        [DataMember]
        public List<LoadoutLite> Loadouts { get; set; }

        [DataMember]
        public List<HeroImage> Images { get; set; }

        internal STUHero STU;

        public Hero(ulong key) {
            STUHero stu = GetInstance<STUHero>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public Hero(STUHero stu, ulong key = default) {
            Init(stu, key);
        }

        private void Init(STUHero hero, ulong key = default) {
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
            IsHero = hero.m_62746D34 > 0;

            if (hero.m_heroLoadout != null) {
                Loadouts = new List<LoadoutLite>();
                foreach (teResourceGUID loadoutGUID in hero.m_heroLoadout) {
                    var loadout = Loadout.GetLoadout(loadoutGUID);
                    if (loadout == null) continue;
                    Loadouts.Add(loadout.ToLite());
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
            var name = GetString(hero.m_0EDCE350);
            return name?.TrimEnd(' ');
        }

        public class HeroImage {
            public teResourceGUID Id { get; set; }
            public teResourceGUID TextureGUID { get; set; }
        }
    }
}