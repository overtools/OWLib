using System;
using System.Collections.Generic;
using DataTool.Flag;
using DataTool.Helper;
using STULib.Types;
using STULib.Types.Gamemodes;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;
using static STULib.Types.Generic.Common;

namespace DataTool.ToolLogic.List {
    [Tool("list-gamemodes", Description = "List game modes", IsSensitive = true, TrackTypes = new ushort[] {0xC7}, CustomFlags = typeof(ListFlags))]
    public class ListGamemodes : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            Log("Gamemodes:");
            var i = 0;
            foreach (var key in TrackedFiles[0xC7]) {
                var thing = GetInstance<STUBrawl>(key);

                if (thing == null) continue;

                var iD = new IndentHelper();
                var name = GetString(thing.Name);
                var subline = GetString(thing.Subline);

                Log($"{iD+1}[{i}] {name}:");
                Log($"{iD+2}Subline: {subline}");

                i++;

                if (thing.Difficulty != null) {
                    Log($"{iD+2}Difficulty: {GetString(thing.Difficulty)}");
                }

                if (thing.DifficultySubline != null) {
                    Log($"{iD + 2}Difficulty Subline: {GetString(thing.DifficultySubline)}");
                }

                if (thing.BrawlName != null) {
                    var brawl = GetInstance<STUBrawlName>(thing.BrawlName);
                    if (brawl != null)
                        Log($"{iD + 2}Brawl: {GetString(brawl.Name)}");
                }

                var subIndent = iD + 2;
                ParseAchievements(subIndent, thing.Achievements);
                ParseCompetitiveInfo(subIndent, thing.CompetitiveInfo);
                ParseUnlocks(subIndent, thing.Achievements);
                ParseInfo(subIndent, thing.Info);
                ParseGamemodeInfo(subIndent, thing.GameModeInfo);
                ParseMaps(subIndent, thing.MapBinding);
                ParseBrawls(subIndent, thing.BrawlInfo);

                Log("\n");
            }
        }

        private static void ParseGamemodeInfo(IndentHelper iD, STUGUID[] gamemodeInfo) {
            if (gamemodeInfo == null) return;
            Log($"{iD}Game Info:");
            foreach (var guid in gamemodeInfo) {
                var something = GetInstance<STUGamemodeBaseInfo>(guid);
                if (something != null)
                    Log($"{iD+1}{GetString(something.Name)}");
            }
        }

        private static void ParseInfo(IndentHelper iD, STUGUID[] info) {
            if (info == null) return;
            Log($"{iD}Info:");
            foreach (var guid in info) {
                Log($"{iD+1}{GetString(guid)}");
            }
        }

        private static void ParseUnlocks(IndentHelper iD, STUGUID[] unlocks) {
            if (unlocks == null) return;
            Log($"{iD}Unlocks:");
            foreach (var guid in unlocks) {
                var unlock = GatherUnlock(guid);
                if (unlock != null)
                    Log($"{iD+1}{unlock.Name} ({unlock.Rarity} {unlock.Type})");
            }
        }

        private static void ParseAchievements(IndentHelper iD, STUGUID[] achievements) {
            if (achievements == null) return;
            Log($"{iD}Achievements:");
            foreach (var guid in achievements) {
                var achievement = GetInstance<STUAchievement>(guid);
                var unlock = GatherUnlock(achievement.Reward);
                if (achievement != null)
                    Log($"{iD+1}{GetString(achievement.Name)} - Reward: {unlock.Name} ({unlock.Rarity} {unlock.Type})");
            }
        }

        private static void ParseCompetitiveInfo(IndentHelper iD, STUBrawlCompetitiveSeasonBase competitiveInfo) {
            if (competitiveInfo == null) return;
            Log($"{iD}Is Comp: true");

            if (competitiveInfo.CompetitorRewards != null) {
                Log($"{iD}Competitor Rewards:");
                foreach (var guid in competitiveInfo.CompetitorRewards.Unlocks) {
                    var unlock = GatherUnlock(guid);
                    if (unlock != null)
                        Log($"{iD+1}{unlock.Name} ({unlock.Rarity} {unlock.Type})");
                }
            }

            if (competitiveInfo.TopRewards != null) {
                Log($"{iD}Top Rewards:");
                foreach (var guid in competitiveInfo.TopRewards.Unlocks) {
                    var unlock = GatherUnlock(guid);
                    if (unlock != null)
                        Log($"{iD+1}{unlock.Name} ({unlock.Rarity} {unlock.Type})");
                }
            }
        }

        private static void ParseMaps(IndentHelper iD, STUGUID mapBinding) {
            if (mapBinding == null) return;
            try {
                var map = GetInstance<STUMapDataBinding>(mapBinding);
                if (map?.MapMetadatas == null) return;

                var mapNames = new List<string>();
                foreach (var guid in map.MapMetadatas) {
                    var mapMeta = GetInstance<STUMap>(guid);
                    if (mapMeta == null) continue;

                    var mapName = GetString(mapMeta.DisplayName);
                    mapNames.Add(mapName);
                }

                Log($"{iD}Maps: {String.Join(", ", mapNames)}");
            } catch { }
        }

        private static void ParseGamemodeData(IndentHelper iD, STUGUID guid) {
            if (guid == null) return;

            var gamemodeData = GetInstance<STUGamemode>(guid);
            Log($"{iD}Gamemode Data:");
            Log($"{iD+1}Type: {GetString(gamemodeData.DisplayName) ?? "N/A"}");

            if (gamemodeData.Teams != null) {
                Log($"{iD+1}Teams:");
                foreach (var team in gamemodeData.Teams) {
                    Log($"{iD+2}{team.TeamType}:");
                    Log($"{iD+3}Max: {team.MaxPlayers}");
                    Log($"{iD+3}Min: {team.MaxPlayers}");

                    if (team.AllowedHeroes != null) {
                        var allowedHeroes = new List<string>();
                        foreach (var heroguid in ((STUGamemodeHeroCollection) team.AllowedHeroes).Heroes) {
                            var hero = GetInstance<STUHero>(heroguid);
                            if (hero == null) continue;
                            allowedHeroes.Add(GetString(hero.Name));
                        }

                        Log($"{iD + 3}Allowed Heroes: {String.Join(", ", allowedHeroes)}");
                    }
                }
            }
        }

        private static void ParseBrawls(IndentHelper iD, STUGUID[] brawls) {
            if (brawls == null) return;
            Log($"{iD}Brawls:");
            var ii = 0;
            foreach (var guid in brawls) {
                var brawlContainer = GetInstance<STUBrawlInfoContainer>(guid);
                if (brawlContainer == null) continue;

                var bName = GetString(brawlContainer.BrawlInfo.Description);
                Log($"{iD+1}[{ii}] {bName}:");
                ii++;
                
                ParseGamemodeData(iD+2, brawlContainer.BrawlInfo.Gamemode);

                if (brawlContainer.BrawlInfo.TeamConfig != null) {
                    var iii = 0;
                    Log($"{iD+2}Team Config:");
                    foreach (var teamConfig in brawlContainer.BrawlInfo.TeamConfig) {
                        Log($"{iD+3}[{iii}]:");

                        iii++;

                        if (teamConfig.BrawlTeamTypeContainer != null) {
                            var teamType = teamConfig.BrawlTeamTypeContainer as STUBrawlTeamType;
                            Log($"{iD+4}Team: {teamType.TeamType}");
                        }

                        if (teamConfig.AllowedHeroes != null) {
                            STUGUID[] heroes = null;
                            if (teamConfig.AllowedHeroes is STUGamemodeHeroCollection)
                                heroes = (teamConfig.AllowedHeroes as STUGamemodeHeroCollection).Heroes;

                            if (teamConfig.AllowedHeroes is STUBrawlHeroCollection)
                                heroes = (teamConfig.AllowedHeroes as STUBrawlHeroCollection).Heroes;

                            var allowedHeroes = new List<string>();
                            foreach (var heroguid in heroes) {
                                var hero = GetInstance<STUHero>(heroguid);
                                if (hero == null) continue;
                                allowedHeroes.Add(GetString(hero.Name));
                            }

                            Log($"{iD+4}Allowed Heroes: {String.Join(", ", allowedHeroes)}");
                        }

                        if (teamConfig.HeroOverrides != null) {
                            Log($"{iD+4}Hero Overrides?:");
                            foreach (var somethingElse in teamConfig.HeroOverrides) {
                                var dsfds = somethingElse.Hero as STUBrawlHero;
                                var hero = GetInstance<STUHero>(dsfds.Hero);
                                Log($"{iD+5}Hero: {GetString(hero.Name)}");
                            }
                        }
                    }
                }
            }
        }
    }
}