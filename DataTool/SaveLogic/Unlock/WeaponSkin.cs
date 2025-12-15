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
                Logger.Warn("\t\tTemporarily enabling texture de-duplication (required for mythic skins)");
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
        SkinTheme.SaveSoundFiles(flags, directory, replacements); // save any sounds to main skin dir
        SaveAnimations(flags, directory, replacements);

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

            if (variantWeaponSkin is STU_475420BE derivedSkin) {
                // ow2 heroes
                // also on the root skin... doesn't really matter
                
                FindLogic.Combo.Find(findInfo, derivedSkin.m_8EB89D4C, variantReplacements);
                FindLogic.Combo.Find(findInfo, derivedSkin.m_56BE636B, variantReplacements, new FindLogic.Combo.ComboContext {
                    // ensure the look is linked to the model
                    Model = derivedSkin.m_8EB89D4C
                });

                findInfo.SetModelName(derivedSkin.m_8EB89D4C, "Combined", variantReplacements);
            }

            FindEffects(findInfo, variantReplacements);
            SkinTheme.SaveSoundFiles(flags, directory, variantReplacements); // save any sounds to main skin dir
            SaveAnimations(flags, directory, variantReplacements);

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

            var loadout = Loadout.Load(weaponEntity.m_loadout);
            if (loadout == null) continue;

            var weaponEntityGUID = weaponEntity.m_entityDefinition;
            info.SetEntityName(weaponEntityGUID, $"{loadout.Name}-{teResourceGUID.Index(weaponEntityGUID)}", weaponReplacements);
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
    
    private static void SaveAnimations(ICLIFlags flags, string directory, Dictionary<ulong, ulong> replacements) {
        // similar story to effects, but we have to diff
        // (as they are overriding whole bend tree sets which contain a lot of base animations too)
        
        FindLogic.Combo.ComboInfo diffInfoBefore = new FindLogic.Combo.ComboInfo();
        FindLogic.Combo.ComboInfo diffInfoAfter = new FindLogic.Combo.ComboInfo();

        foreach (KeyValuePair<ulong, ulong> replacement in replacements) {
            uint type = teResourceGUID.Type(replacement.Value);
            if (type != 0x6 && type != 0x20 && type != 0x21) {
                // animation, blend tree, blend tree set
                continue;
            }
            
            // note: passing replacements will break this (it would walk skinned only)
            // although, this could also be technically wrong, if things inside the blend trees/set could be skinned
            FindLogic.Combo.Find(diffInfoBefore, replacement.Key);
            FindLogic.Combo.Find(diffInfoAfter, replacement.Key, replacements);
        }
        
        FindLogic.Combo.ComboInfo onlySkinContent = new FindLogic.Combo.ComboInfo();
        foreach (var skinAnimation in diffInfoAfter.m_animations.Keys) {
            if (diffInfoBefore.m_animations.ContainsKey(skinAnimation)) {
                // skip, this animation is the same in the base skin
                continue;
            }
            
            FindLogic.Combo.Find(onlySkinContent, skinAnimation, replacements);
        }
        
        // save all models and entities referenced by skin animations
        // this can include non-skinned animations for child models/entities
        var saveContext = new Combo.SaveContext(onlySkinContent);
        Combo.Save(flags, directory, saveContext);

        // todo: i'm not enabling this for clarity. it's not obvious that animations would be filtered like this
        // clear out any animations that are tied to models (therefore extracted above)
        // foreach (var savedModel in onlySkinContent.m_models.Values) {
        //     foreach (var savedAnimation in savedModel.m_animations) {
        //         onlySkinContent.m_animations.Remove(savedAnimation);
        //     }
        // }
        
        // save all animations that aren't tied to a model
        // (automatically appends "Animations" dir)
        Combo.SaveAllAnimations(flags, directory, saveContext);
    }
}