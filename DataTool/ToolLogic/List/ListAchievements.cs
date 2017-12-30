using System;
using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using Newtonsoft.Json;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-achievements", Description = "List achievements", TrackTypes = new ushort[] {0x68, 0x75}, CustomFlags = typeof(ListFlags))]
    public class ListAchievements : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class AchievementInfo {
            public string Name;
            public string Group;
            public string Description;
            public string Hero;
            public Reward Reward;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong GUID;
            
            public AchievementInfo(ulong guid, string name, string group, string description, string hero, Reward reward) {
                GUID = guid;
                Name = name;
                Group = group;
                Description = description;
                Hero = hero;
                Reward = reward;
            }
        }
        
        [JsonObject(MemberSerialization.OptOut)]
        public class Reward {
            public string Name;
            public string Type;
            public string Rarity;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong GUID;

            public Reward(ulong guid, string name, string type, string rarity) {
                GUID = guid;
                Name = name;
                Type = type;
                Rarity = rarity;
            }
        }

        public void Parse(ICLIFlags toolFlags) {
            List<AchievementInfo> achievements = GetAchievements();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(achievements, flags);
                    return;
                }

            foreach (AchievementInfo achievement in achievements) {
                var iD = new IndentHelper();
                
                Log($"{achievement.Name}");
                
                if (achievement.Hero != null)
                    Log($"{iD+1} Hero: {achievement.Hero}");
                
                Log($"{iD+1} Group: {achievement.Group}");
                Log($"{iD+1} Description: {achievement.Description}");
                
                if (achievement.Reward != null)
                    Log($"{iD+1} Reward: {achievement.Reward.Name} ({achievement.Reward.Rarity} {achievement.Reward.Type})");

                Log();
            }
        }

        public List<AchievementInfo> GetAchievements() {
            Log("Fetching achievements");
            List<AchievementInfo> achievementList = new List<AchievementInfo>();
            IDictionary<ulong, string> heroItemMapping = GetHeroItemMapping();

            Log("Mapping achievement data \n");

            foreach (ulong key in TrackedFiles[0x68]) {
                STUAchievement achievement = GetInstance<STUAchievement>(key);
                if (achievement == null) continue;

                string name = GetString(achievement.Name);
                string desc = GetString(achievement.Description);
                string group = achievement.Category.ToString();
                ItemInfo item = GatherUnlock(achievement.Reward);
                Reward reward = new Reward(achievement.Reward, item.Name, item.Type, item.Rarity);
                heroItemMapping.TryGetValue(item.GUID, out string hero);
                
                achievementList.Add(new AchievementInfo(key, name, group, desc, hero, reward));
            }

            return achievementList;
        }

        private static IDictionary<ulong, string> GetHeroItemMapping() {
            Log("Generating hero to item mapping");
            Dictionary<ulong, string> @return = new Dictionary<ulong, string>();

            foreach (var key in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(key);
                if (hero?.LootboxUnlocks == null) continue;

                string heroName = GetString(hero.Name);

                Dictionary<string, HashSet<ItemInfo>> unlocks = ListHeroUnlocks.GetUnlocksForHero(hero.LootboxUnlocks);
                
                foreach (KeyValuePair<string, HashSet<ItemInfo>> unlockPair in unlocks) {
                    if (unlockPair.Value?.Count == 0 || unlockPair.Value == null) {
                        continue;
                    }
                    
                    foreach (ItemInfo unlock in unlockPair.Value) {
                        @return[unlock.GUID] = heroName;
                    }
                }
            }

            return @return;
        }
    }
}
