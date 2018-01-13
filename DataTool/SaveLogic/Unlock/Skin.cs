using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using DataTool.DataModels;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using STULib.Types.Generic;
using STULib.Types.Statescript.Components;
using STULib.Types.STUUnlock;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;

namespace DataTool.SaveLogic.Unlock {
    public class Skin {
        public static void Save(ICLIFlags flags, string skinName, string basePath, STUHero hero, string rarity, STUSkinOverride skinOverride, List<ItemInfo> weaponSkins, List<STULoadout> abilities, bool quiet = true) {
            string heroName = GetString(hero.Name);
            string heroNamePath = GetValidFilename(heroName) ?? "Unknown";
            heroNamePath = heroNamePath.TrimEnd(' ');
            
            string path = Path.Combine(basePath,
                $"{heroNamePath}\\Skins\\{rarity}\\{GetValidFilename(skinName)}");
            
            Dictionary<uint, ItemInfo> realWeaponSkins = new Dictionary<uint, ItemInfo>();
            if (weaponSkins != null) {
                foreach (ItemInfo weaponSkin in weaponSkins) {
                    realWeaponSkins[((Weapon) weaponSkin.Unlock).Index] = weaponSkin;
                }
            }
            
            Dictionary<ulong, ulong> replacements = skinOverride.ProperReplacements;
            
            LoudLog("\tFinding");
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, hero.EntityMain, replacements);
            FindLogic.Combo.Find(info, hero.EntityHeroSelect, replacements);
            FindLogic.Combo.Find(info, hero.EntityHighlightIntro, replacements);
            FindLogic.Combo.Find(info, hero.EntityPlayable, replacements);
            FindLogic.Combo.Find(info, hero.EntityThirdPerson, replacements);

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
                    weaponOverride.ProperReplacements?.ToDictionary(x => x.Key, y => y.Value) ??
                    new Dictionary<ulong, ulong>();

                List<STUHero.WeaponEntity> weaponEntities = new List<STUHero.WeaponEntity>();
                if (hero.WeaponComponents1 != null) {
                    weaponEntities.AddRange(hero.WeaponComponents1);
                }
                if (hero.WeaponComponents2 != null) {
                    weaponEntities.AddRange(hero.WeaponComponents2);
                }
                foreach (STUHero.WeaponEntity heroWeapon in weaponEntities) {
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
                FindLogic.Combo.Find(info, guiImage, replacements);
            }
            Combo.SaveLooseTextures(flags, Path.Combine(path, "GUI"), info);

            info.SetEntityName(hero.EntityHeroSelect, $"{heroName}-HeroSelect");
            info.SetEntityName(hero.EntityPlayable, $"{heroName}-Playable-ThirdPerson");
            info.SetEntityName(hero.EntityThirdPerson, $"{heroName}-ThirdPerson");
            info.SetEntityName(hero.EntityMain, $"{heroName}-Base");
            info.SetEntityName(hero.EntityHighlightIntro, $"{heroName}-HighlightIntro");

            // this doesn't work yet.
            // todo: I need a good way to compare voice masters
            /*string soundDirectory = Path.Combine(basePath, "Sound");
            FindLogic.Combo.ComboInfo diffInfo = new FindLogic.Combo.ComboInfo();
            foreach (KeyValuePair<ulong,ulong> replacement in replacements) {
                uint diffReplacementIndex = GUID.Index(replacement.Value);
                if (diffReplacementIndex == 0x2C || diffReplacementIndex == 0x5F || diffReplacementIndex == 0x3F || diffReplacementIndex == 0xB2) {
                    FindLogic.Combo.Find(diffInfo, replacement.Value);
                    FindLogic.Combo.Find(info, replacement.Key);
                }
            }

            foreach (KeyValuePair<ulong,FindLogic.Combo.SoundFileInfo> diffInfoSoundFile in diffInfo.SoundFiles) {
                if (info.SoundFiles.ContainsKey(diffInfoSoundFile.Key)) continue;
                Combo.SaveSoundFile(flags, soundDirectory, diffInfo, diffInfoSoundFile.Key, false);
            }
            
            foreach (KeyValuePair<ulong,FindLogic.Combo.SoundFileInfo> diffInfoSoundFile in diffInfo.VoiceSoundFiles) {
                if (info.VoiceSoundFiles.ContainsKey(diffInfoSoundFile.Key)) continue;
                Combo.SaveSoundFile(flags, soundDirectory, diffInfo, diffInfoSoundFile.Key, true);
            }*/
            
            LoudLog("\tSaving");
            Combo.Save(flags, path, info);            
            LoudLog("\tDone");
        }
        
        public static void Save(ICLIFlags flags, string path, STUHero hero, STUHero.Skin skin, bool quiet = true) {
            STUSkinOverride skinOverride = GetInstance<STUSkinOverride>(skin.SkinOverride);
            LoudLog($"Extracting skin {GetString(hero.Name)} {GetFileName(skin.SkinOverride)}");
            Save(flags, GetFileName(skin.SkinOverride), path, hero, "", skinOverride, null, null, quiet);
        }

        public static void Save(ICLIFlags flags, string path, STUHero hero, string rarity, STULib.Types.STUUnlock.Skin skin, List<ItemInfo> weaponSkins, List<STULoadout> abilities, bool quiet=true) {
            if (skin == null) return;
            LoudLog($"Extracting skin {GetString(hero.Name)} {GetString(skin.CosmeticName)}");
            if (weaponSkins == null) weaponSkins = new List<ItemInfo>();
            
            STUSkinOverride skinOverride = GetInstance<STUSkinOverride>(skin.SkinResource);
            Save(flags, GetString(skin.CosmeticName).TrimEnd(' '), path, hero, rarity, skinOverride, weaponSkins, abilities, quiet);
        }
    }
}