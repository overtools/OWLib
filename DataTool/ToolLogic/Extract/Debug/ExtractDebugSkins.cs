using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.SaveLogic.Unlock;
using DataTool.ToolLogic.List;
using STULib.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-skin", Description = "Extract skins (debug)", TrackTypes = new ushort[] {0x75}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugSkins : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetHeroes(toolFlags);
        }

        public void GetHeroes(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            
            foreach (ulong key in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(key);
                if (hero == null) continue;
                
                Dictionary<string, HashSet<ItemInfo>> unlocks = ListHeroUnlocks.GetUnlocksForHero(hero.LootboxUnlocks);
                if (unlocks == null) continue;
                List<ItemInfo> skins = unlocks.SelectMany(x => x.Value.Where(y => y.Type == "Skin")).ToList();
                List<ItemInfo> weaponSkins = unlocks.SelectMany(x => x.Value.Where(y => y.Type == "Weapon")).ToList();

                foreach (ItemInfo skin in skins) {
                    Skin.Save(flags, $"{basePath}\\HeroSkinDebug", hero, skin.Rarity, skin.Unlock as STULib.Types.STUUnlock.Skin, weaponSkins, null, false);
                }
            }
        }
    }
}