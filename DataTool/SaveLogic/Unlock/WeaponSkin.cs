using System.Collections.Generic;
using System.IO;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.SaveLogic.Unlock; 

public static class WeaponSkin {
    public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock, STUHero hero) {
        STU_2448F3AA weaponVariant = (STU_2448F3AA) unlock.STU;
        var replacements = SkinTheme.GetReplacements(weaponVariant.m_A9736011);

        FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
        FindWeapons(info, replacements, hero.m_previewWeaponEntities);
        FindWeapons(info, replacements, hero.m_C2FE396F);
        SkinTheme.FindSoundFiles(flags, directory, replacements);

        var context = new Combo.SaveContext(info);
        Combo.Save(flags, directory, context);
    }

    private static void FindWeapons(FindLogic.Combo.ComboInfo info, Dictionary<ulong, ulong> weaponReplacements, STU_A0872511[] entities) {
        if (entities == null) return;
        foreach (STU_A0872511 weaponEntity in entities) {
            FindLogic.Combo.Find(info, weaponEntity.m_entityDefinition, weaponReplacements);

            if (weaponEntity.m_loadout == 0) continue;
            Loadout loadout = new Loadout(weaponEntity.m_loadout);
            if (loadout.GUID == 0) continue;
            info.SetEntityName(weaponEntity.m_entityDefinition, $"{loadout.Name}-{teResourceGUID.Index(weaponEntity.m_entityDefinition)}");
        }
    }
}