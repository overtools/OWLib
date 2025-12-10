using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Util;
using TankLib;
using static DataTool.Program;
using Map = DataTool.SaveLogic.Map;

namespace DataTool.ToolLogic.Extract;

[Tool("extract-maps", Aliases = ["extract-map"], Description = "Extract maps", CustomFlags = typeof(ExtractFlags))]
public class ExtractMaps : QueryParser, ITool, IQueryParser {
    public string DynamicChoicesKey => UtilDynamicChoices.VALID_MAP_NAMES;

    public void Parse(ICLIFlags toolFlags) {
        SaveMaps(toolFlags);
    }

    protected override void QueryHelp(List<QueryType> types) {
        IndentHelper indent = new IndentHelper();
        Log("Please specify what you want to extract:");
        Log($"{indent + 1}Command format: \"{{map name}}\" ");
        Log($"{indent + 1}Each query should be surrounded by \", and individual queries should be separated by spaces");


        Log($"{indent + 1}Maps can be listed using the \"list-maps\" mode");
        Log($"{indent + 1}All map names are in your selected locale");

        Log("\r\nExample of map name argument: ");
        Log($"{indent + 1}\"Kings Row\"");
        Log($"{indent + 1}\"Ilios\" \"Kanezaka:D86\"");

        Log("\r\nFull example: ");
        Log($"{indent + 1}\"C:\\Program Files(x86)\\Overwatch\" extract-maps \"C:\\Output_Path\" Oasis");
    }

    public void SaveMaps(ICLIFlags toolFlags) {
        var flags = (ExtractFlags) toolFlags;
        flags.EnsureOutputDirectory();

        Dictionary<string, ParsedHero> parsedTypes = ParseQuery(flags, QueryTypes, QueryNameOverrides);
        if (parsedTypes == null) {
            QueryHelp(QueryTypes);
            return;
        }

        foreach (ulong key in TrackedFiles[0x9F]) {
            var mapInfo = MapHeader.Load(key);
            if (mapInfo == null) continue;
            var map = mapInfo.STU;
            mapInfo.Name = mapInfo.Name ?? "Title Screen";

            Dictionary<string, ParsedArg> config = GetQuery(parsedTypes, mapInfo.Name, mapInfo.VariantName,
                                                            mapInfo.GetUniqueName(), mapInfo.GetName(), teResourceGUID.Index(map.m_map).ToString("X"), "*");

            if (config.Count == 0) continue;

            Map.Save(flags, mapInfo, map, key, flags.OutputPath);
            SaveScratchDatabase();
        }
        
        LogUnknownQueries(parsedTypes);
    }

    public List<QueryType> QueryTypes => new List<QueryType>();

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static readonly IgnoreCaseDict<string> QueryNameOverrides = new IgnoreCaseDict<string>() {
        ["horizon"] = "Horizon Lunar Colony",
        ["moon"] = "horizon Lunar Colony",
        ["hlc"] = "Horizon Lunar Colony",
        ["anubis"] = "Temple of Anubis",
        ["gibraltar"] = "Watchpoint: Gibraltar",
        ["watchpoint"] = "Watchpoint: Gibraltar",
        ["watchpoint gibraltar"] = "Watchpoint: Gibraltar",
        ["lijiang"] = "Lijiang Tower",
        ["estadio das ras"] = "Estádio Das Rãs",
        ["chateau guillard"] = "Château Guillard",
        ["chateau"] = "Château Guillard",
        ["ecopoint"] = "Ecopoint: Antarctica",
        ["antarctica"] = "Ecopoint: Antarctica",
        ["ecopoint antarctica"] = "Ecopoint: Antarctica",
        ["volskaya"] = "Volskaya Industries",
        ["kings row"] = "King's Row",
        ["paraiso"] = "Paraíso",
        ["rio"] = "Paraíso",
        ["esperanca"] = "Esperança",
        ["portugal"] = "Esperança",
        ["atlis"] = "Aatlis",
        ["atlas"] = "Aatlis",
        ["Circuit Royale"] = "Circuit Royal", // (its lowercase royal in game...)
        ["Monte Carworld"] = "Circuit Royal",
        ["Wuxing University"] = "Wuxing University - Water College",
        ["Wuxing"] = "Wuxing University - Water College",
    };
}