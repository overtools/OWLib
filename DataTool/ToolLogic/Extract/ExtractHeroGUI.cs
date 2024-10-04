using System.Collections.Generic;
using System.IO;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.SaveLogic;
using TankLib;
using TankLib.Helpers;
using TankLib.STU.Types;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-hero-icons", Aliases = new [] { "extract-hero-gui" }, Description = "Extract hero GUI images", CustomFlags = typeof(ExtractFlags))]
    public class ExrtactHeroGUI : ITool {
        private const string Container = "HeroIcons";

        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            var basePath = flags.OutputPath;
            flags.EnsureOutputDirectory();

            var heroIntelImageMapping = GetIntelDatabaseHeroImages();
            var heroProgressionImageMapping = GetHeroProgressionImageMapping();

            foreach (var (key, hero) in Helpers.GetHeroes()) {
                var heroStu = hero.STU;
                var heroNameLower = hero.Name?.ToLowerInvariant().Trim();
                var heroCleanName = IO.GetValidFilename(hero.Name);

                if (string.IsNullOrEmpty(heroCleanName)) {
                    continue;
                }

                Logger.Log($"Processing {heroCleanName}");

                var heroImageCombo = new FindLogic.Combo.ComboInfo();
                if (hero.Images != null) {
                    foreach (var heroImage in hero.Images) {
                        FindLogic.Combo.Find(heroImageCombo, heroImage.TextureGUID);
                        //heroImageCombo.SetTextureName(heroImage.TextureGUID, teResourceGUID.AsString(heroImage.Id));
                    }
                }

                if (heroIntelImageMapping.TryGetValue(heroNameLower, out var value)) {
                    foreach (var intelImage in value) {
                        FindLogic.Combo.Find(heroImageCombo, intelImage);
                    }
                }

                if (heroProgressionImageMapping.TryGetValue(hero.GUID, out var progressionImages)) {
                    foreach (var progressionImage in progressionImages) {
                        FindLogic.Combo.Find(heroImageCombo, progressionImage);
                    }
                }

                Combo.SaveLooseTextures(flags, Path.Combine(basePath, Container, heroCleanName), new Combo.SaveContext(heroImageCombo), new Combo.SaveTextureOptions {
                    FileTypeOverride = "png",
                    ProcessIcon = true,
                });

                if (hero.Loadouts != null) {
                    var heroLoadoutCombo = new FindLogic.Combo.ComboInfo();
                    foreach (var heroLoadout in hero.Loadouts) {
                        FindLogic.Combo.Find(heroLoadoutCombo, heroLoadout.TextureGUID);
                        heroLoadoutCombo.SetTextureName(heroLoadout.TextureGUID, heroLoadout.Name);
                    }

                    Combo.SaveLooseTextures(flags, Path.Combine(basePath, Container, heroCleanName, "Abilities"), new Combo.SaveContext(heroLoadoutCombo), new Combo.SaveTextureOptions {
                        FileTypeOverride = "png"
                    });
                }

                if (heroStu.m_FC833C02 != null) {
                    var combo = new FindLogic.Combo.ComboInfo();
                    foreach (var statThing in heroStu.m_FC833C02) {
                        FindLogic.Combo.Find(combo, statThing.m_D176E9FD);
                    }

                    Combo.SaveLooseTextures(flags, Path.Combine(basePath, Container, heroCleanName, "StatIcons"), new Combo.SaveContext(combo), new Combo.SaveTextureOptions {
                        FileTypeOverride = "png"
                    });
                }

                // skins don't have icons anymore so pointless
                /*foreach (var unlock in hero.GetUnlocks().IterateUnlocks()) {
                    if (unlock.Type != UnlockType.Skin || unlock.SkinThemeGUID == 0) {
                        continue;
                    }

                    var skinTheme = STUHelper.GetInstance<STUSkinTheme>(unlock.SkinThemeGUID);
                    if (skinTheme == null) {
                        Logger.Log($"Can't save {unlock.Name} as it has no skin theme");
                        continue;
                    }


                    var filePath = Path.Combine(basePath, Container, heroCleanName, "Skins", unlock.IsEsportsUnlock ? "OWL" : String.Empty);
                    var replacements = SaveLogic.Unlock.SkinTheme.GetReplacements(skinTheme);
                    FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
                    var saveContext = new Combo.SaveContext(info);

                    hero.STU.m_8203BFE1 ??= Array.Empty<STU_1A496D3C>(); // todo: fix the gosh darn stu
                    foreach (STU_1A496D3C tex in hero.STU.m_8203BFE1) { // find GUI
                        if (tex.m_id != 0x0D800000000040C8) {
                            continue;
                        }

                        var guid = FindLogic.Combo.GetReplacement(tex.m_texture, replacements);
                        FindLogic.Combo.Find(info, guid);
                        info.SetTextureOptions(guid, new Combo.SaveTextureOptions {
                            FileTypeOverride = "png",
                            ProcessIcon = true,
                            FileNameOverride = $"{teResourceGUID.Index(unlock.GUID)} - Icon - {IO.GetValidFilename(unlock.Name)}"
                        });
                    }

                    if (skinTheme.m_ECCC4A5D != null) {
                        FindLogic.Combo.Find(info, skinTheme.m_ECCC4A5D);
                        info.SetTextureOptions(skinTheme.m_ECCC4A5D, new Combo.SaveTextureOptions {
                            FileTypeOverride = "png",
                            ProcessIcon = true,
                            FileNameOverride = $"{teResourceGUID.Index(unlock.GUID)} - Portrait - {IO.GetValidFilename(unlock.Name)}"
                        });
                    }

                    Combo.SaveLooseTextures(flags, filePath, saveContext);
                }*/
            }
        }

        private static Dictionary<string, List<teResourceGUID>> GetIntelDatabaseHeroImages() {
            /**
             * Fetches images for heroes and NPCs from their Bios in the Intel Database
             * Hero intel entries have their own STU but there's no way to differentiate between a Bio or Journal entry which generally refers to a mission
             * Hacky fix for this is to just skip images that are referenced by multiple heroes as they're likely to be mission images
             *
             * Null Sector NPCs use a general STU that contains a lot of general lore so you can't filter based on the STU
             * however general lore entries have an id that indicates what category they belong to so we can filter based on that
             *
             * Images are stored in a dictionary of hero/npc name to images. Hero names are from the hero stu directly so they can be easily looked up
             * However Null Sector entries aren't linked to the NPCs hero meaning the mapping will only work if the name of the lore entry is the same as the NPC's Hero name
             */

            var imageMapping = new Dictionary<string, List<teResourceGUID>>();
            var foundImageCache = new HashSet<teResourceGUID>();
            var duplicateImageSet = new HashSet<teResourceGUID>();

            foreach (var key in Program.TrackedFiles[0x14B]) {
                var stu = STUHelper.GetInstance<STU_BFEFD7C8>(key);
                if (stu == null) {
                    continue;
                }

                string heroName = null;
                switch (stu) {
                    case STU_D75C45C2 heroStu: {
                        var hero = new Hero(heroStu.m_hero);
                        if (hero.GUID == 0 || stu?.m_1B3F1138 == null) {
                            continue;
                        }

                        heroName = hero.Name?.ToLowerInvariant().Trim();
                        break;
                    }
                    case STU_1640A86B generalIntelStu:
                        // null sector lore entries
                        if (generalIntelStu.m_4F9CF442?.m_id != 0x0D80000000002788) {
                            continue;
                        }

                        heroName = IO.GetString(generalIntelStu.m_93E355A7)?.ToLowerInvariant().Trim();
                        break;
                    default:
                        continue;
                }

                if (string.IsNullOrEmpty(heroName)) {
                    continue;
                }

                var imageGuid = stu.m_1B3F1138;

                if (foundImageCache.Contains(imageGuid)) {
                    duplicateImageSet.Add(imageGuid);
                    continue;
                }

                foundImageCache.Add(imageGuid);
                if (!imageMapping.ContainsKey(heroName)) {
                    imageMapping[heroName] = new List<teResourceGUID>();
                }

                imageMapping[heroName].Add(imageGuid);
            }

            // remove images that are referenced by multiple heroes
            foreach (var (guid, list) in imageMapping) {
                list.RemoveAll(x => duplicateImageSet.Contains(x));
            }

            return imageMapping;
        }

        private static Dictionary<teResourceGUID, List<teResourceGUID>> GetHeroProgressionImageMapping() {
            var imageMapping = new Dictionary<teResourceGUID, List<teResourceGUID>>();

            foreach (var key in Program.TrackedFiles[0x165]) {
                var stu = STUHelper.GetInstance<STU_80B85097>(key);
                if (stu == null) continue;
                if (stu.m_hero == 0) continue;

                if (!imageMapping.TryGetValue(stu.m_hero, out var imageList)) {
                    imageList = new List<teResourceGUID>();
                    imageMapping.Add(stu.m_hero, imageList);
                }
                if (stu.m_5AE357B7 != 0) imageList.Add(stu.m_5AE357B7); // bg
                if (stu.m_910D7A6A != 0) imageList.Add(stu.m_910D7A6A); // progression update portrait
            }

            return imageMapping;
        }
    }
}