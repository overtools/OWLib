using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using STULib.Types.Generic;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using Texture = DataTool.SaveLogic.Texture;

namespace DataTool.ToolLogic.Extract {

    public class ExtractHeroUnlocksFlags : ExtractFlags {
        // naughty? I don't override anything here because I can just steal positionals
    }
    // syntax:
    // *|skin=Classic
    // *|skin=Classic
    // *|skin=*,!Classic
    // *
    // "Soldier: 76|skin=Classic,Daredevil: 76|emote=Pushups|victory pose=*"
    // "Soldier: 76|skin=Classic,Daredevil: 76|emote=Pushups|victory pose=*" Roadhog
    // "Soldier: 76|skin=Classic,Daredevil: 76|emote=Pushups|victory pose=*" Roadhog|emote=Dance  // assume nothing else for roadhog, be specific
    // "Soldier: 76|skin=!Classic"  // invalid
    // "Soldier: 76|skin=!Classic,*"  // valid
    // Roadhog
    [Tool("extract-unlocks", Description = "Extract all heroes sprays and icons", TrackTypes = new ushort[] { 0x75 }, CustomFlags = typeof(ExtractFlags))]
    public class ExtractHeroUnlocks : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        string rootDir = "Heroes";

        public void Parse(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            var heroes = GetHeroes();
            SaveUnlocksForHeroes(flags, heroes, basePath);
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

        public void SaveUnlocksForHeroes(ICLIFlags flags, List<STUHero> heroes, string basePath) {
            List<string> extractTypes = new List<string> {"skin", "spray", "icon"};  // todo
            foreach (var hero in heroes) {
                var heroName = GetValidFilename(GetString(hero.Name));
                var unlocks = GetInstance<STUHeroUnlocks>(hero.LootboxUnlocks);
                if (unlocks?.Unlocks == null || heroName == null)
                    continue;

                List<STUAbilityInfo> abilities = new List<STUAbilityInfo>();
                foreach (Common.STUGUID ability in hero.Abilities) {
                    STUAbilityInfo abilityInfo = GetInstance<STUAbilityInfo>(ability);
                    if (abilityInfo != null) abilities.Add(abilityInfo);
                }
                
                List<ItemInfo> weaponSkins = List.ListHeroUnlocks.GetUnlocksForHero(hero.LootboxUnlocks, false).SelectMany(x => x.Value.Where(y => y.Type == "Weapon")).ToList(); // eww?

                var achievementUnlocks = GatherUnlocks(unlocks.SystemUnlocks?.Unlocks?.Select(it => (ulong)it)).ToList();
                SaveLogic.Unlock.SprayAndImage.SaveItems(basePath, heroName, rootDir, "Achievements", null, achievementUnlocks);
                foreach (ItemInfo achievementUnlock in achievementUnlocks) {  // todo @zb: make a convenience fucntion
                    if (achievementUnlock.Type != "Skin") continue;
                    SaveLogic.Unlock.Skin.Save(flags, basePath, hero, $"Achievement\\{achievementUnlock.Rarity}", achievementUnlock.Unlock as STULib.Types.STUUnlock.Skin, weaponSkins, abilities, false);
                }
                

                foreach (var defaultUnlocks in unlocks.Unlocks)  {
                    var dUnlocks = GatherUnlocks(defaultUnlocks.Unlocks.Select(it => (ulong) it)).ToList();
                    SaveLogic.Unlock.SprayAndImage.SaveItems(basePath, heroName, rootDir, "Standard", null, dUnlocks);

                    foreach (ItemInfo itemInfo in dUnlocks) {
                        if (itemInfo.Type == "Skin") {
                            SaveLogic.Unlock.Skin.Save(flags, basePath, hero, itemInfo.Rarity, itemInfo.Unlock as STULib.Types.STUUnlock.Skin, weaponSkins, abilities, false);
                        }
                    }
                }

                foreach (var eventUnlocks in unlocks.LootboxUnlocks) {
                    if (eventUnlocks?.Data?.Unlocks == null) continue;

                    var eventKey = ItemEvents.GetInstance().EventsNormal[(uint)eventUnlocks.Event];
                    var eUnlocks = eventUnlocks.Data.Unlocks.Select(it => GatherUnlock((ulong)it)).ToList();
                    SaveLogic.Unlock.SprayAndImage.SaveItems(basePath, heroName, rootDir, eventKey, null, eUnlocks);

                    foreach (ItemInfo itemInfo in eUnlocks) {
                        if (itemInfo.Type == "Skin") {
                            SaveLogic.Unlock.Skin.Save(flags, basePath, hero, itemInfo.Rarity, itemInfo.Unlock as STULib.Types.STUUnlock.Skin, weaponSkins, abilities, false);
                        }
                    }
                }

                var heroTextures = new Dictionary<ulong, List<TextureInfo>>();
                heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.ImageResource1, "Icon", true);
                heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.ImageResource2, "Portrait", true);
                heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.ImageResource3, "unknown", true); // Same as Icon for now
                heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.ImageResource4, "Avatar", true);
                Texture.Save(null, Path.Combine(basePath, rootDir, heroName, "GUI"), heroTextures);
            }
        }
    }
}