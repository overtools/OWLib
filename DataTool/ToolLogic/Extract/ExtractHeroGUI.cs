using System;
using System.IO;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.SaveLogic;
using DirectXTexNet;
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
                        FileTypeOverride = "tif",
                        ProcessIcon = true,
                        DXGIFormatOverride = DXGI_FORMAT.R8G8B8A8_UNORM,
                    });
                }

                if (hero.Loadouts != null) {
                    var heroLoadoutCombo = new FindLogic.Combo.ComboInfo();
                    foreach (var heroLoadout in hero.Loadouts) {
                        FindLogic.Combo.Find(heroLoadoutCombo, heroLoadout.TextureGUID);
                        heroLoadoutCombo.SetTextureName(heroLoadout.TextureGUID, heroLoadout.Name);
                    }

                    Combo.SaveLooseTextures(flags, Path.Combine(basePath, Container, heroCleanName, "Abilities"), new Combo.SaveContext(heroLoadoutCombo), new Combo.SaveTextureOptions {
                        FileTypeOverride = "tif",
                        DXGIFormatOverride = DXGI_FORMAT.R8G8B8A8_UNORM,
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


                    var filePath = Path.Combine(basePath, Container, heroCleanName, "Skins", unlock.IsEsportsUnlock ? "OWL" : String.Empty);
                    var replacements = SaveLogic.Unlock.SkinTheme.GetReplacements(skinTheme);
                    FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
                    var saveContext = new Combo.SaveContext(info);

                    foreach (STU_1A496D3C tex in hero.STU.m_8203BFE1) { // find GUI
                        if (tex.m_id != 0x0D800000000040C8) {
                            continue;
                        }

                        var guid = FindLogic.Combo.GetReplacement(tex.m_texture, replacements);
                        FindLogic.Combo.Find(info, guid);
                        info.SetTextureOptions(guid, new Combo.SaveTextureOptions {
                            FileTypeOverride = "tif",
                            ProcessIcon = true,
                            DXGIFormatOverride = DXGI_FORMAT.R8G8B8A8_UNORM,
                            FileNameOverride = $"{teResourceGUID.Index(unlock.GUID)} - Icon - {IO.GetValidFilename(unlock.Name)}"
                        });
                    }

                    if (skinTheme.m_ECCC4A5D != null) {
                        FindLogic.Combo.Find(info, skinTheme.m_ECCC4A5D);
                        info.SetTextureOptions(skinTheme.m_ECCC4A5D, new Combo.SaveTextureOptions {
                            FileTypeOverride = "tif",
                            ProcessIcon = true,
                            DXGIFormatOverride = DXGI_FORMAT.R8G8B8A8_UNORM,
                            FileNameOverride = $"{teResourceGUID.Index(unlock.GUID)} - Portrait - {IO.GetValidFilename(unlock.Name)}"
                        });
                    }

                    Combo.SaveLooseTextures(flags, filePath, saveContext);
                }
            }
        }
    }
}