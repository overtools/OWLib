using System.Collections.Generic;
using System.IO;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using SixLabors.ImageSharp;
using TankLib;
using TankLib.Helpers;
using TankLib.STU.Types;

namespace DataTool.SaveLogic.Unlock;

public static class WeaponSkin {
    public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock, STUHero hero) {
        STU_2448F3AA weaponSkinUnlock = (STU_2448F3AA) unlock.STU;
        var weaponSkinGUID = weaponSkinUnlock.m_A9736011;
        var weaponSkinSTU = STUHelper.GetInstance<STUSkinBase>(weaponSkinGUID);

        if (weaponSkinSTU is STU_4BC3E632) {
            Logger.Log($"\tExtracting mythic weapon skin {unlock.Name}");

            var wasDeduping = Program.Flags.Deduplicate;
            if (!wasDeduping) {
                Logger.Warn("\t\tTemporarily enabling texture deduplication");
            }
            Program.Flags.Deduplicate = true;

            SaveMythicWeaponSkin(flags, directory, hero, weaponSkinGUID);

            Program.Flags.Deduplicate = wasDeduping;
        } else {
            Logger.Log($"\tExtracting weapon skin {unlock.Name}");
            SaveNormalWeaponSkin(flags, directory, hero, weaponSkinGUID);
        }
    }

    private static void SaveNormalWeaponSkin(ICLIFlags flags, string directory, STUHero hero, teResourceGUID weaponSkinGUID) {
        var replacements = SkinTheme.GetReplacements(weaponSkinGUID);

        FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
        FindWeapons(info, replacements, hero);
        FindEffects(info, replacements);
        SkinTheme.FindSoundFiles(flags, directory, replacements); // save any sounds to main skin dir

        var context = new Combo.SaveContext(info) {
            m_saveAnimationEffectsAsLoose = true
        };
        Combo.Save(flags, directory, context);
    }

    private static void SaveMythicWeaponSkin(ICLIFlags flags, string directory, STUHero hero, teResourceGUID mythicSkinGUID) {
        var mythicSkin = STUHelper.GetInstance<STU_4BC3E632>(mythicSkinGUID);

        FindLogic.Combo.ComboInfo findInfo = new FindLogic.Combo.ComboInfo();
        var saveContext = new Combo.SaveContext(findInfo) {
            m_saveAnimationEffectsAsLoose = true
        };
        var partTextures = MythicSkin.LoadPartTextures(mythicSkin, findInfo);

        foreach (var partVariantIndices in MythicSkin.IteratePermutations(mythicSkin)) {
            var variantSkinGUID = MythicSkin.BuildVariantGUID(mythicSkinGUID, mythicSkin, partVariantIndices, 0x167);

            var variantWeaponSkin = STUHelper.GetInstance<STUSkinBase>(variantSkinGUID);
            if (variantWeaponSkin == null) {
                Logger.Warn("WeaponSkin", $"couldn't load mythic weapon skin permutation {variantSkinGUID} for {teResourceGUID.AsString(mythicSkinGUID)}. shouldn't happen");
                return;
            }

            var variantDirectoryName = MythicSkin.BuildVariantName(mythicSkin, partVariantIndices);
            Logger.Debug("WeaponSkin", $"Processing mythic variant {variantDirectoryName}");
            var variantDirectory = Path.Combine(directory, variantDirectoryName);

            var variantReplacements = SkinTheme.GetReplacements(variantSkinGUID);

            findInfo.m_entities.Clear(); // sanity
            FindWeapons(findInfo, variantReplacements, hero);
            MythicSkin.SaveAndFlushEntities(flags, findInfo, saveContext, variantDirectory);

            FindEffects(findInfo, variantReplacements);
            SkinTheme.FindSoundFiles(flags, directory, SkinTheme.GetReplacements(variantSkinGUID)); // save any sounds to main skin dir

            // todo: the part textures seem to not be set... bound demon = hanzo mythic bow
            using var infoTexture = MythicSkin.BuildVariantInfoImage(partVariantIndices, partTextures);
            infoTexture?.SaveAsPng(Path.Combine(variantDirectory, "Info.png"));
        }

        Combo.Save(flags, directory, saveContext);
    }

    private static void FindWeapons(FindLogic.Combo.ComboInfo info, Dictionary<ulong, ulong> weaponReplacements, STUHero hero) {
        FindWeapons(info, weaponReplacements, hero.m_previewWeaponEntities);
        FindWeapons(info, weaponReplacements, hero.m_C2FE396F);
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

    private static void FindEffects(FindLogic.Combo.ComboInfo info, Dictionary<ulong, ulong> replacements) {
        // for weapon skins we don't save the whole hero, only preview weapon entities
        // because of this, no effects are saved automatically
        
        // instead, manually locate effect replacements
        // (which means we will only save replaced things, not every sound from the hero)
        
        foreach (KeyValuePair<ulong, ulong> replacement in replacements) {
            uint type = teResourceGUID.Type(replacement.Value);
            if (type != 0xD && type != 0x8F) {
                // effect, animation effect
                continue;
            }
            
            FindLogic.Combo.Find(info, replacement.Value);
        }
    }
}