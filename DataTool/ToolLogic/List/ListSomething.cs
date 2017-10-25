using System;
using System.Diagnostics;
using DataTool.Flag;
using DataTool.Helper;
using STULib.Types;
using STULib.Types.Gamemodes;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.List {
    [Tool("list-stuff", Description = "List subtitles", TrackTypes = new ushort[] {0xC7}, CustomFlags = typeof(ListFlags))]
    public class ListSomething : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            Log("Gamemodes:");
            var i = 0;
            foreach (var key in TrackedFiles[0xC7]) {
                var thing = GetInstance<STUGamemodeBase>(key);

                if (thing == null) continue;

                var iD = new IndentHelper();
                var name = GetString(thing.Name);
                var subline = GetString(thing.Subline);

                //if (name == "Copa Lúcioball") Debugger.Break();

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

                if (thing.Achievements != null) {
                    Log($"{iD+2}Achievements:");
                    foreach (var guid in thing.Achievements) {
                        var achievement = GetInstance<STUAchievement>(guid);
                        var unlock = GatherUnlock(achievement.Reward);
                        if (achievement != null)
                            Log($"{iD + 3}{GetString(achievement.Name)} - Reward: {unlock.Name} ({unlock.Rarity} {unlock.Type})");
                    }
                }

                if (thing.CompetitiveInfo != null) {
                    Log($"{iD+2}Is Comp: true");

                    if (thing.CompetitiveInfo.CompetitorRewards != null) {
                        Log($"{iD+2}Competitor Rewards:");
                        foreach (var guid in thing.CompetitiveInfo.CompetitorRewards.Unlocks) {
                            var unlock = GatherUnlock(guid);
                            if (unlock != null)
                                Log($"{iD+3}{unlock.Name} ({unlock.Rarity} {unlock.Type})");
                        }
                    }

                    if (thing.CompetitiveInfo.TopRewards != null) {
                        Log($"{iD+2}Top Rewards:");
                        foreach (var guid in thing.CompetitiveInfo.TopRewards.Unlocks) {
                            var unlock = GatherUnlock(guid);
                            if (unlock != null)
                                Log($"{iD+3}{unlock.Name} ({unlock.Rarity} {unlock.Type})");
                        }
                    }
                }

                if (thing.Unlocks != null) {
                    Log($"{iD+2}Unlocks:");
                    foreach (var guid in thing.Unlocks) {
                        var unlock = GatherUnlock(guid);
                        if (unlock != null)
                            Log($"{iD+3}{unlock.Name} ({unlock.Rarity} {unlock.Type})");
                    }
                }

                if (thing.Info != null) {
                    Log($"{iD+2}Info:");
                    foreach (var guid in thing.Info) {
                        Log($"{iD+3}{GetString(guid)}");
                    }
                }

                if (thing.GameModeInfo != null) {
                    Log($"{iD+2}Game Info:");
                    foreach (var guid in thing.GameModeInfo) {
                        var something = GetInstance<STUGamemodeBaseInfo>(guid);
                        if (something != null)
                            Log($"{iD+3}{GetString(something.Name)}");
                    }
                }


                Debugger.Break();
                Log("\n");
            }
        }
    }
}