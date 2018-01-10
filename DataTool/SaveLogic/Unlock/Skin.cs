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
using STULib.Types.Statescript.Components;
using STULib.Types.STUUnlock;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;

namespace DataTool.SaveLogic.Unlock {
    public class Skin {
        public static void Save(ICLIFlags flags, string skinName, string path, STUHero hero, string rarity, STUSkinOverride skinOverride, List<ItemInfo> weaponSkins, List<STULoadout> abilities, bool quiet = true) {
            string heroName = GetString(hero.Name);
            string heroPath = GetValidFilename(heroName);
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
            
            Dictionary<ulong, ulong> realReplacements = skinOverride.ProperReplacements;
            
            LoudLog("\tFinding");
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, hero.EntityMain, realReplacements);
            FindLogic.Combo.Find(info, hero.EntityHeroSelect, realReplacements);
            FindLogic.Combo.Find(info, hero.EntityHighlightIntro, realReplacements);
            FindLogic.Combo.Find(info, hero.EntityPlayable, realReplacements);
            FindLogic.Combo.Find(info, hero.EntityThirdPerson, realReplacements);

            info.Config.DoExistingEntities = true;
            
            uint replacementIndex = 0;
            foreach (Common.STUGUID weaponOverrideGUID in skinOverride.Weapons) {
                STUHeroWeapon weaponOverride = GetInstance<STUHeroWeapon>(weaponOverrideGUID);
                if (weaponOverride == null) continue;

                string weaponSkinName = null;
                if (realWeaponSkins.ContainsKey(replacementIndex)) {
                    weaponSkinName = GetValidFilename(GetString(realWeaponSkins[replacementIndex].Unlock.CosmeticName));
                }

                Dictionary<ulong, ulong> weaponReplacements =
                    weaponOverride.ProperReplacements?.ToDictionary(x => (ulong) x.Key, y => (ulong) y.Value) ??
                    new Dictionary<ulong, ulong>();

                foreach (STUHero.WeaponEntity heroWeapon in hero.WeaponComponents1.Concat(hero.WeaponComponents2)) {
                    FindLogic.Combo.Find(info, heroWeapon.Entity, weaponReplacements);
                    STUModelComponent modelComponent = GetInstance<STUModelComponent>(heroWeapon.Entity);
                    if (modelComponent?.Look == null || weaponSkinName == null) continue;
                    ulong modelLook = FindLogic.Combo.GetReplacement(modelComponent.Look, weaponReplacements);
                    if (!info.ModelLooks.ContainsKey(modelLook)) continue;
                    FindLogic.Combo.ModelLookInfo modelLookInfo = info.ModelLooks[modelLook];
                    modelLookInfo.Name = weaponSkinName;
                }
                
                replacementIndex++;
            }
            info.Config.DoExistingEntities = false;

            foreach (Common.STUGUID guiImage in new[] {hero.ImageResource1, hero.ImageResource2, hero.ImageResource3, 
                hero.ImageResource3, hero.ImageResource4, skinOverride.SkinImage, hero.SpectatorIcon}) {
                FindLogic.Combo.Find(info, guiImage, realReplacements);
            }
            SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(basePath, "GUI"), info);

            info.SetEntityName(hero.EntityHeroSelect, $"{heroName}-HeroSelect");
            info.SetEntityName(hero.EntityPlayable, $"{heroName}-Playable-ThirdPerson");
            info.SetEntityName(hero.EntityThirdPerson, $"{heroName}-ThirdPerson");
            info.SetEntityName(hero.EntityMain, $"{heroName}-Base");
            info.SetEntityName(hero.EntityHighlightIntro, $"{heroName}-HighlightIntro");
            
            LoudLog("\tSaving");
            SaveLogic.Combo.Save(flags, basePath, info);            
            LoudLog("\tDone");
            
            /*            
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
            }*/
        }
        
        public static void Save(ICLIFlags flags, string path, STUHero hero, STUHero.Skin skin, bool quiet = true) {
            STUSkinOverride skinOverride = GetInstance<STUSkinOverride>(skin.SkinOverride);
            if (!quiet) Log($"Extracting skin {GetString(hero.Name)} {GetFileName(skin.SkinOverride)}");
            Save(flags, GetFileName(skin.SkinOverride), path, hero, "", skinOverride, null, null, quiet);
        }

        public static void Save(ICLIFlags flags, string path, STUHero hero, string rarity, STULib.Types.STUUnlock.Skin skin, List<ItemInfo> weaponSkins, List<STULoadout> abilities, bool quiet=true) {
            if (skin == null) return;
            if (!quiet) Log($"Extracting skin {GetString(hero.Name)} {GetString(skin.CosmeticName)}");
            if (weaponSkins == null) weaponSkins = new List<ItemInfo>();
            
            STUSkinOverride skinOverride = GetInstance<STUSkinOverride>(skin.SkinResource);
            Save(flags, GetString(skin.CosmeticName).TrimEnd(' '), path, hero, rarity, skinOverride, weaponSkins, abilities, quiet);
        }
    }
}