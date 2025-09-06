#nullable enable
using System.Linq;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels.Hero;

public class Loadout {
    public teResourceGUID GUID { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public LoadoutCategory Category { get; set; }
    public string? Button { get; set; }
    public string? ButtonUnk { get; set; }
    public string[]? DescriptionButtons { get; set; }
    public teResourceGUID MovieGUID { get; set; }
    public teResourceGUID TextureGUID { get; set; }
    public bool IsHiddenAbility { get; set; }
    public bool IsSecondaryWeapon { get; set; }

    public Loadout(STULoadout? stu, ulong key = default) {
        Init(stu, key);
    }

    public void Init(STULoadout? loadout, ulong key = default) {
        if (loadout == null) return;

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

    public static Loadout? Load(ulong guid) {
        var stu = STUHelper.GetInstance<STULoadout>(guid);
        if (stu == null) return null;

        return new Loadout(stu, guid);
    }
}

public class LoadoutLite {
    public string? Name { get; set; }
    public string? Description { get; set; }
    public LoadoutCategory Category { get; set; }
    public teResourceGUID MovieGUID { get; set; }
    public teResourceGUID TextureGUID { get; set; }

    public LoadoutLite(Loadout loadout) {
        Name = loadout.Name;
        Description = loadout.Description;
        Category = loadout.Category;
        MovieGUID = loadout.MovieGUID;
        TextureGUID = loadout.TextureGUID;
    }
}