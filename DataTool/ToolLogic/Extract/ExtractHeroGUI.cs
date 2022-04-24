using System;
using System.IO;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.SaveLogic;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-hero-icons", Description = "Extract hero icons", CustomFlags = typeof(ExtractFlags))]
    public class ExrtactHeroGUI : ITool {
        private const string Container = "HeroIcons";

        public void Parse(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            foreach (var (key, heroStu) in Helpers.GetHeroes()) {
                var hero = new Hero(heroStu, key);
                var heroCleanName = IO.GetValidFilename(hero.Name);
                if (string.IsNullOrEmpty(heroCleanName)) {
                    continue;
                }

                Logger.Log($"Processing {heroCleanName}");

                if (hero.Images != null) {
                    var heroImageCombo = new FindLogic.Combo.ComboInfo();
                    foreach (var heroImage in hero.Images) {
                        FindLogic.Combo.Find(heroImageCombo, heroImage.TextureGUID);
                        heroImageCombo.SetTextureName(heroImage.TextureGUID, teResourceGUID.AsString(heroImage.Id));
                    }

                    Combo.SaveLooseTextures(flags, Path.Combine(basePath, Container, heroCleanName), new Combo.SaveContext(heroImageCombo), new Combo.SaveTextureOptions {
                        FileTypeOverride = "png",
                        ProcessIcon = true,
                    });
                }

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

                foreach (var unlock in hero.GetUnlocks().IterateUnlocks()) {
                    if (unlock.Type != UnlockType.Skin || unlock.SkinThemeGUID == 0) {
                        continue;
                    }

                    var skinTheme = STUHelper.GetInstance<STUSkinTheme>(unlock.SkinThemeGUID);
                    if (skinTheme == null) {
                        Logger.Log($"Can't save {unlock.Name} as it has no skin theme");
                        continue;
                    }

                    if (skinTheme.m_ECCC4A5D == null) {
                        Logger.Log($"Can't save {unlock.Name} as it has no image");
                        continue;
                    }

                    FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
                    FindLogic.Combo.Find(info, skinTheme.m_ECCC4A5D);
                    var filePath = Path.Combine(basePath, Container, heroCleanName, "Skins", unlock.IsEsportsUnlock ? "OWL" : String.Empty);
                    var saveContext = new Combo.SaveContext(info);

                    Combo.SaveTexture(flags, filePath, saveContext, skinTheme.m_ECCC4A5D, new Combo.SaveTextureOptions {
                        FileTypeOverride = "png",
                        ProcessIcon = true,
                        FileNameOverride = $"{teResourceGUID.Index(unlock.GUID)} - {IO.GetValidFilename(unlock.Name)}"
                    });
                }
            }
        }
    }
}