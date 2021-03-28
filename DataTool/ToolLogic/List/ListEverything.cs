using System;
using DataTool.Flag;
using TankLib.STU.Types;
using System.Collections.Generic;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;
using DataTool.ToolLogic.List.Misc;

namespace DataTool.ToolLogic.List {
    [Tool("list-everything", Description = "List everything", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListEverything : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            List<ITool> tools = new List<ITool> {
            new ListArcadeModes(),
            new ListBrawlName(),
            new ListBrawls(),
            new ListChatReplacements(),
            new ListChatSettings(),
            new ListConversations(),
            new ListEsportTeams(),
            new ListGameModes(),
            new ListGameRulesets(),
            new ListGameRulesetSchemas(),
            new ListHeroesRulesets(),
            new ListHeroesRulesets(),
            new ListProfanityFilters(),
            new ListReportResponses(),
            new ListTips(),
            new ListWorkshop(),
            new ListAbilities(),
            new ListAchievements(),
            new ListAllUnlocks(),
            new ListGeneralUnlocks(),
            new ListHeroes(),
            new ListHeroUnlocks(),
            new ListLootbox(),
            new ListMaps(),
            new ListResourceKeys(),
            new ListSubtitlesProper()
        };
        foreach (var tool in tools) {
                Log(tool.GetType().Name + '\n');
                tool.Parse(toolFlags);
            }
        }
    }
}
