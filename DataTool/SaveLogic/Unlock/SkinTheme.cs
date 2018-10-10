using System.Collections.Generic;
using System.IO;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.SaveLogic.Unlock {
    public static class SkinTheme {
        public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock, STUHero hero) {
            if (!(unlock.STU is STUUnlock_SkinTheme unlockSkinTheme)) return;
            STUSkinTheme skinTheme = GetInstance<STUSkinTheme>(unlockSkinTheme.m_skinTheme);
            if (skinTheme == null) return;
            
            LoudLog($"\tExtracting skin {unlock.Name}");
            Save(flags, directory, skinTheme, hero);
        }

        public static void Save(ICLIFlags flags, string directory, STU_63172E83 skin, STUHero hero) {
            STUSkinTheme skinTheme = GetInstance<STUSkinTheme>(skin.m_5E9665E3);
            if (skinTheme == null) return;
            LoudLog($"\tExtracting skin {IO.GetFileName(skin.m_5E9665E3)}");
            Save(flags, directory, skinTheme, hero);
        }

        public static void Save(ICLIFlags flags, string directory, STUSkinBase skin, STUHero hero) {
            Dictionary<ulong, ulong> replacements = GetReplacements(skin);
            
            LoudLog("\t\tFinding");
            
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            
            FindLogic.Combo.Find(info, hero.m_gameplayEntity, replacements);
            info.SetEntityName(hero.m_gameplayEntity, "Gameplay3P");
            
            FindLogic.Combo.Find(info, hero.m_previewEmoteEntity, replacements);
            info.SetEntityName(hero.m_previewEmoteEntity, "PreviewEmote");
            
            FindLogic.Combo.Find(info, hero.m_322C521A, replacements);
            info.SetEntityName(hero.m_322C521A, "Showcase");
            
            FindLogic.Combo.Find(info, hero.m_26D71549, replacements);
            info.SetEntityName(hero.m_26D71549, "HeroGallery");
            
            FindLogic.Combo.Find(info, hero.m_8125713E, replacements);
            info.SetEntityName(hero.m_8125713E, "HighlightIntro");
            
            if (skin is STUSkinTheme skinTheme) {
                info.Config.DoExistingEntities = true;
                foreach (var weaponOverrideGUID in skinTheme.m_heroWeapons) {
                    STUHeroWeapon heroWeapon = GetInstance<STUHeroWeapon>(weaponOverrideGUID);
                    if (heroWeapon == null) continue;

                    Dictionary<ulong, ulong> weaponReplacements = GetReplacements(heroWeapon);

                    SavePreviewWeapons(info, weaponReplacements, hero.m_previewWeaponEntities);
                    SavePreviewWeapons(info, weaponReplacements, hero.m_C2FE396F);
                }
                info.Config.DoExistingEntities = false;
            }

            foreach (STU_1A496D3C tex in hero.m_8203BFE1) {
                FindLogic.Combo.Find(info, tex.m_texture, replacements);
                info.SetTextureName(tex.m_texture, teResourceGUID.AsString(tex.m_id));
            }
            
            Combo.SaveLooseTextures(flags, Path.Combine(directory, "GUI"), info);

            if (replacements != null) {
                string soundDirectory = Path.Combine(directory, "Sound");
            
                FindLogic.Combo.ComboInfo diffInfoBefore = new FindLogic.Combo.ComboInfo();
                FindLogic.Combo.ComboInfo diffInfoAfter = new FindLogic.Combo.ComboInfo();
                
                foreach (KeyValuePair<ulong,ulong> replacement in replacements) {
                    uint diffReplacementType = teResourceGUID.Type(replacement.Value);
                    if (diffReplacementType != 0x2C && diffReplacementType != 0x3F &&
                        diffReplacementType != 0xB2) continue; // no voice sets, use extract-hero-voice
                    FindLogic.Combo.Find(diffInfoAfter, replacement.Value);
                    FindLogic.Combo.Find(diffInfoBefore, replacement.Key);
                }
                
                foreach (KeyValuePair<ulong, FindLogic.Combo.VoiceSetInfo> voiceSet in diffInfoAfter.VoiceSets) {
                    if (diffInfoBefore.VoiceSets.ContainsKey(voiceSet.Key)) continue;
                    Combo.SaveVoiceSet(flags, soundDirectory, diffInfoAfter, voiceSet.Key);
                }

                foreach (KeyValuePair<ulong,FindLogic.Combo.SoundFileInfo> soundFile in diffInfoAfter.SoundFiles) {
                    if (diffInfoBefore.SoundFiles.ContainsKey(soundFile.Key)) continue;
                    Combo.SaveSoundFile(flags, soundDirectory, diffInfoAfter, soundFile.Key, false);
                }
            
                foreach (KeyValuePair<ulong,FindLogic.Combo.SoundFileInfo> soundFile in diffInfoAfter.VoiceSoundFiles) {
                    if (diffInfoBefore.VoiceSoundFiles.ContainsKey(soundFile.Key)) continue;
                    Combo.SaveSoundFile(flags, soundDirectory, diffInfoAfter, soundFile.Key, true);
                }
            }

            LoudLog("\t\tSaving");
            Combo.Save(flags, directory, info);
            LoudLog("\t\tDone");
        }

        private static void SavePreviewWeapons(FindLogic.Combo.ComboInfo info, Dictionary<ulong, ulong> weaponReplacements, STU_A0872511[] entities) {
            if (entities == null) return;
            foreach (STU_A0872511 weaponEntity in entities) {
                FindLogic.Combo.Find(info, weaponEntity.m_entityDefinition, weaponReplacements);

                if (weaponEntity.m_loadout == 0) continue;
                Loadout loadout = new Loadout(weaponEntity.m_loadout);
                info.SetEntityName(weaponEntity.m_entityDefinition, $"{loadout.Name}-{teResourceGUID.Index(weaponEntity.m_entityDefinition)}");
            }
        }

        public static Dictionary<ulong, ulong> GetReplacements(STUSkinBase skin) {
            if (skin.m_runtimeOverrides != null) {
                Dictionary<ulong, ulong> replacements = new Dictionary<ulong, ulong>();
                foreach (KeyValuePair<ulong,STUSkinRuntimeOverride> @override in skin.m_runtimeOverrides) {
                    replacements[@override.Key] = @override.Value.m_3D884507;
                }
                return replacements;
            }
            return null;
        }
    }
}