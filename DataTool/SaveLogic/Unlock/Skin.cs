using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using STULib.Types.Enums;
using STULib.Types.Generic;
using STULib.Types.STUUnlock;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;

namespace DataTool.SaveLogic.Unlock {
    public class Skin {
        public static void Save(ICLIFlags flags, string path, STUHero hero, STUHero.Skin skin, bool quiet = true) {
            STUSkinOverride skinOverride = GetInstance<STUSkinOverride>(skin.SkinOverride);
            if (!quiet) Log($"Extracting skin {GetString(hero.Name)} {GetFileName(skin.SkinOverride)}");
            if (!quiet) Log("\tFinding models");
            Save(flags, GetFileName(skin.SkinOverride), path, hero, "", skinOverride, null, null, quiet);
        }

        public static void Save(ICLIFlags flags, string skinName, string path, STUHero hero, string rarity, STUSkinOverride skinOverride, List<ItemInfo> weaponSkins, List<STULoadout> abilities, bool quiet = true) {
            string heroPath = GetValidFilename(GetString(hero.Name));
            if (heroPath == null) heroPath = "Unknown";
            heroPath = heroPath.TrimEnd(' ');
            string basePath = Path.Combine(path,
                $"{heroPath}\\Skins\\{rarity}\\{GetValidFilename(skinName)}");
            Dictionary<uint, ItemInfo> realWeaponSkins = new Dictionary<uint, ItemInfo>();
            if (weaponSkins != null) {
                foreach (ItemInfo weaponSkin in weaponSkins) {
                    realWeaponSkins[((Weapon) weaponSkin.Unlock).Index] = weaponSkin;
                }
            }
            
            Dictionary<Common.STUGUID, Common.STUGUID> replacements = skinOverride.ProperReplacements ?? new Dictionary<Common.STUGUID, Common.STUGUID>();
            Dictionary<ulong, ulong> realReplacements = replacements.ToDictionary(x => (ulong)x.Key, y => (ulong)y.Value);
            HashSet<ModelInfo> models = new HashSet<ModelInfo>();
            models = FindLogic.Model.FindModels(models, hero.StatescriptHeroComponent1, realReplacements);
            models = FindLogic.Model.FindModels(models, hero.StatescriptHeroComponent2, realReplacements);
            models = FindLogic.Model.FindModels(models, hero.StatescriptHeroComponent3, realReplacements);
            models = FindLogic.Model.FindModels(models, hero.StatescriptHeroComponent4, realReplacements);
            models = FindLogic.Model.FindModels(models, hero.StatescriptHeroComponent5, realReplacements);

            if (hero.WeaponComponents1 != null) {
                foreach (STUHero.Statescript statescript in hero.WeaponComponents1) {
                    models = FindLogic.Model.FindModels(models, statescript.Component, realReplacements);
                }
            }
            
            if (hero.WeaponComponents2 != null) {
                foreach (STUHero.Statescript statescript in hero.WeaponComponents2    ) {
                    models = FindLogic.Model.FindModels(models, statescript.Component, realReplacements);
                }
            }
            
            if (!quiet) Log("\tFinding sounds");
            Dictionary<ulong, List<SoundInfo>> newSounds = new Dictionary<ulong, List<SoundInfo>>();
            Dictionary<ulong, List<SoundInfo>> oldSounds = new Dictionary<ulong, List<SoundInfo>>();
            Dictionary<ulong, ulong> soundParentConvesion = new Dictionary<ulong, ulong>();  // used to ignore differences in toplevelKey
            uint soundIndex = 1;
            foreach (KeyValuePair<Common.STUGUID,Common.STUGUID> replacement in replacements) {
                soundParentConvesion[soundIndex] = replacement.Value;
                newSounds = FindLogic.Sound.FindSounds(newSounds, replacement.Key, null, false, soundIndex, realReplacements);
                oldSounds = FindLogic.Sound.FindSounds(oldSounds, replacement.Key, null, false, soundIndex);
                soundIndex++;
            }
            
            if (!quiet) Log("\tFinding weapons");
            Dictionary<ulong, HashSet<ModelInfo>> weaponModels = new Dictionary<ulong, HashSet<ModelInfo>>();
            Dictionary<int, string> weaponNamesRaw = new Dictionary<int, string>();
            Dictionary<int, string> weaponNames = new Dictionary<int, string>();

            if (abilities != null) {
                foreach (STULoadout ability in abilities) {
                    if (ability.Category != LoadoutCategory.Weapon) continue;
                    if (!weaponNamesRaw.ContainsKey(ability.WeaponIndex-1))
                        weaponNamesRaw[ability.WeaponIndex-1] = GetString(ability.Name);
                }
            }
            
            int weaponCount = 0;
            if (hero.WeaponComponents1 != null) weaponCount = hero.WeaponComponents1.Length;
            if (hero.WeaponComponents2 != null) weaponCount = hero.WeaponComponents2.Length;
            foreach (Common.STUGUID overrideWeapon in skinOverride.Weapons) {
                // WeaponComponents1 and WeaponComponents2 are weapon components, matching up with Weapon.Inex
                // doomfist doesn't have WeaponComponents1, also his weapon ids are messed up (linked?)
                
                STUHeroWeapon weaponOverride = GetInstance<STUHeroWeapon>(overrideWeapon);
                
                if (weaponOverride == null) continue;
                
                Dictionary<ulong, ulong> subRealReplacements = weaponOverride?.ProperReplacements?.ToDictionary(x => (ulong)x.Key, y => (ulong)y.Value) ?? new Dictionary<ulong, ulong>();
                weaponModels[overrideWeapon] = new HashSet<ModelInfo>();

                for (int i = 0; i < weaponCount; i++) {
                    Common.STUGUID weaponComponent = null;
                    if (hero.WeaponComponents2 != null && hero.WeaponComponents2.Length > i)
                        weaponComponent = hero.WeaponComponents2[i].Component;
                    if (hero.WeaponComponents1 != null && hero.WeaponComponents1.Length > i)
                        weaponComponent = hero.WeaponComponents1[i].Component;
                    // disabled for now:
                    // if (weaponNamesRaw.ContainsKey(i)) {
                    //     weaponNames[i] = weaponNamesRaw[i];
                    // }
                    weaponModels[overrideWeapon] = FindLogic.Model.FindModels(weaponModels[overrideWeapon], weaponComponent, subRealReplacements);
                    foreach (ModelInfo modelInfo in weaponModels[overrideWeapon]) {  // this feels dirty
                        // sometimes animations seem to be defined in the context of another model
                        ModelInfo main = models.FirstOrDefault(x => Equals(x.GUID, modelInfo.GUID));
                        if (main == null) continue;
                        foreach (AnimationInfo mainAnimation in main.Animations) {
                            if (!modelInfo.Animations.Contains(mainAnimation)) {
                                modelInfo.Animations.Add(mainAnimation);
                            }
                        }
                    }
                }
            }
            
            if (!quiet) Log("\tFinding GUI");
            Dictionary<ulong, List<TextureInfo>> guiTextures = new Dictionary<ulong, List<TextureInfo>>();
            foreach (Common.STUGUID existingGui in new [] {hero.ImageResource1, hero.ImageResource2, hero.ImageResource3, hero.ImageResource3, hero.ImageResource4}) {
                if (existingGui == null) continue;
                if (replacements.ContainsKey(existingGui)) {
                    guiTextures = FindLogic.Texture.FindTextures(guiTextures, replacements[existingGui], null, true,
                        realReplacements);
                }
            }
            
            if (!quiet) Log("\tProcessing sounds");
            if (newSounds.Count != 0) {
                // find new sounds
                Dictionary<ulong, List<SoundInfo>> extractSounds = new Dictionary<ulong, List<SoundInfo>>();
                foreach (KeyValuePair<ulong,List<SoundInfo>> keyValuePair in newSounds) {
                    if (!oldSounds.ContainsKey(keyValuePair.Key)) {
                        extractSounds[keyValuePair.Key] = keyValuePair.Value;
                    } else {
                        foreach (SoundInfo newSound in keyValuePair.Value) {
                            if (!oldSounds[keyValuePair.Key].Contains(newSound)) {
                                if (!extractSounds.ContainsKey(keyValuePair.Key)) extractSounds[keyValuePair.Key] = new List<SoundInfo>();
                                extractSounds[keyValuePair.Key].Add(newSound);
                            }
                        }
                    }
                }
                
                foreach (KeyValuePair<ulong, List<SoundInfo>> extractSoundPair in extractSounds.ToDictionary(x => x.Key, x => x.Value)) {
                    extractSounds.Remove(extractSoundPair.Key);
                    extractSounds[soundParentConvesion[extractSoundPair.Key]] = extractSoundPair.Value;
                }
                if (!quiet) Log("\tSaving sounds");
                Sound.Save(flags, Path.Combine(basePath, "Sounds"), extractSounds);
            }

            if (!quiet && models.Count > 0) Log("\tSaving models");
            foreach (ModelInfo model in models) {
                Model.Save(flags, Path.Combine(basePath, "Models"), model, $"{GetString(hero.Name)} Skin {skinName}_{GUID.Index(model.GUID):X}");
            }
            
            if (!quiet && weaponModels.Count > 0) Log("\tSaving weapons");
            uint weaponIndex = 0;
            foreach (KeyValuePair<ulong,HashSet<ModelInfo>> weapon in weaponModels) {
                int weaponModelIndex = 0;
                foreach (ModelInfo model in weapon.Value) {
                    string weaponName = GetFileName(weapon.Key);
                    string modelName = null;
                    if (weaponNames.ContainsKey(weaponModelIndex)) modelName = GetValidFilename(weaponNames[weaponModelIndex].TrimEnd());
                    if (realWeaponSkins.ContainsKey(weaponIndex)) weaponName = GetValidFilename(GetString(realWeaponSkins[weaponIndex].Unlock.CosmeticName));
                    Model.Save(flags, Path.Combine(basePath, $"Weapons\\{weaponName}\\"), model, $"{GetString(hero.Name)} Skin {skinName} Weapon {GUID.Index(weapon.Key)}_{GUID.Index(model.GUID):X}", modelName);
                    weaponModelIndex++;
                }
                weaponIndex++;
            }

            guiTextures = FindLogic.Texture.FindTextures(guiTextures, skinOverride.SkinImage, null, true, realReplacements);
            if (guiTextures.Count > 0) {
                if (!quiet) Log("\tSaving GUI");
                Texture.Save(flags, Path.Combine(basePath, "GUI"), guiTextures);
            }
            if (!quiet) Log("\tComplete");
            
        }

        public static void Save(ICLIFlags flags, string path, STUHero hero, string rarity, STULib.Types.STUUnlock.Skin skin, List<ItemInfo> weaponSkins, List<STULoadout> abilities, bool quiet=true) {
            if (!quiet) Log($"Extracting skin {GetString(hero.Name)} {GetString(skin.CosmeticName)}");
            if (!quiet) Log("\tFinding models");
            if (weaponSkins == null) weaponSkins = new List<ItemInfo>();
            
            STUSkinOverride skinOverride = GetInstance<STUSkinOverride>(skin.SkinResource);
            Save(flags, GetString(skin.CosmeticName), path, hero, rarity, skinOverride, weaponSkins, abilities, quiet);
        }
    }
}