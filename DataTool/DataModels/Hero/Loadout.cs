using System.Linq;
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
        public teResourceGUID GUID;

        [DataMember]
        public string Name;

        [DataMember]
        public string Description;

        [DataMember]
        public LoadoutCategory Category;

        [DataMember]
        public string Button;

        [DataMember]
        public string ButtonUnk;

        [DataMember]
        public string[] DescriptionButtons;

        [DataMember]
        public teResourceGUID MovieGUID;

        [DataMember]
        public teResourceGUID TextureGUID;

        [DataMember]
        public bool IsHiddenAbility;

        [DataMember]
        public bool IsSecondaryWeapon;

        public Loadout(ulong key) {
            STULoadout stu = STUHelper.GetInstance<STULoadout>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public Loadout(STULoadout stu) {
            Init(stu);
        }

        public static Loadout GetLoadout(ulong key) {
            STULoadout loadout = STUHelper.GetInstance<STULoadout>(key);
            if (loadout == null) return null;
            return new Loadout(loadout);
        }

        public void Init(STULoadout loadout, ulong key = default) {
            GUID = (teResourceGUID) key;
            MovieGUID = loadout.m_infoMovie;
            TextureGUID = loadout.m_texture;
            Category = loadout.m_category;

            Name = GetString(loadout.m_name);
            Description = GetString(loadout.m_description);

            Button = GetString(STUHelper.GetInstance<STU_C5243F93>(loadout.m_logicalButton)?.m_name);
            ButtonUnk = GetString(STUHelper.GetInstance<STU_C5243F93>(loadout.m_9290B942)?.m_name);
            DescriptionButtons = loadout.m_B1124918?.Select(x => GetString(STUHelper.GetInstance<STU_C5243F93>(x)?.m_name)).ToArray();

            // If the ability isn't shown in the UI (weapons, zoom ability)
            IsHiddenAbility = loadout.m_0E679979 >= 1;

            // Mercy, Bastion and Torbjorn all have 2 weapons, this is only set on their secondary weapons??
            IsSecondaryWeapon = loadout.m_0E679979 == 2;
        }

        public LoadoutLite ToLite() {
            return new LoadoutLite(this);
        }
    }

    public class LoadoutLite {
        [DataMember]
        public string Name;

        [DataMember]
        public string Description;

        [DataMember]
        public LoadoutCategory Category;

        [DataMember]
        public teResourceGUID MovieGUID;

        [DataMember]
        public teResourceGUID TextureGUID;

        public LoadoutLite(Loadout loadout) {
            Name = loadout.Name;
            Description = loadout.Description;
            Category = loadout.Category;
            MovieGUID = loadout.MovieGUID;
            TextureGUID = loadout.TextureGUID;
        }
    }
}
