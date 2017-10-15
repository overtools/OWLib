using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using Texture = DataTool.SaveLogic.Texture;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-unlocks", Description = "Extract all heroes sprays and icons", TrackTypes = new ushort[] {0x54},
        CustomFlags = typeof(ExtractFlags))]
    public class ExtractHeroUnlocks : ITool
    {
        public void IntegrateView(object sender)
        {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags)
        {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            var heroes = GetHeroes();
            SaveUnlocksForHeroes(heroes, basePath);
            Debugger.Break();
        }

        public List<STUHero> GetHeroes() {
            var @return = new List<STUHero>();
            foreach (ulong key in TrackedFiles[0x75]) {
                var hero = GetInstance<STUHero>(key);
                if (hero?.Name == null || hero.LootboxUnlocks == null) continue;

                @return.Add(hero);
            }

            return @return;
        }

        public void SaveUnlocksForHeroes(List<STUHero> heroes, string basePath) {
            foreach (var hero in heroes)
            {
                var heroName = GetValidFilename(GetString(hero.Name));
                var unlocks = GetInstance<STUHeroUnlocks>(hero.LootboxUnlocks);
                if (unlocks?.Unlocks == null || heroName == null)
                    continue;

                var achievementUnlocks = GatherUnlocks(unlocks.SystemUnlocks?.Unlocks?.Select(it => (ulong)it)).ToList();
                SaveLogic.Unlock.SprayAndImage.SaveItems(basePath, heroName, "Heroes", "Achievements", null, achievementUnlocks);

                foreach (var defaultUnlocks in unlocks.Unlocks)  {
                    var dUnlocks = GatherUnlocks(defaultUnlocks.Unlocks.Select(it => (ulong) it)).ToList();
                    SaveLogic.Unlock.SprayAndImage.SaveItems(basePath, heroName, "Heroes", "Standard", null, dUnlocks);
                }

                foreach (var eventUnlocks in unlocks.LootboxUnlocks) {
                    if (eventUnlocks?.Data?.Unlocks == null) continue;

                    var eventKey = ItemEvents.GetInstance().EventsNormal[(uint)eventUnlocks.Event];
                    var eUnlocks = eventUnlocks.Data.Unlocks.Select(it => GatherUnlock((ulong)it)).ToList();
                    SaveLogic.Unlock.SprayAndImage.SaveItems(basePath, heroName, "Heroes", eventKey, null, eUnlocks);
                }

                var heroTextures = new Dictionary<ulong, List<TextureInfo>>();
                heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.ImageResource1, "Icon", true);
                heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.ImageResource2, "Portrait", true);
                heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.ImageResource3, "unknown", true); // Same as Icon for now
                heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.ImageResource4, "Avatar", true);
                Texture.Save(null, Path.Combine(basePath, "Heroes", heroName), heroTextures);
            }
        }
    }
}