using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.List;
using DataTool.ToolLogic.Util;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using Map = DataTool.SaveLogic.Map;
using static DataTool.Helper.SpellCheckUtils;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-maps", Description = "Extract maps", CustomFlags = typeof(ExtractFlags))]
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

            Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes = ParseQuery(flags, QueryTypes, QueryNameOverrides);
            if (parsedTypes == null) {
                QueryHelp(QueryTypes);
                return;
            }

            FillMapSpellDict(SymSpell);
            SpellCheckMapName(parsedTypes,SymSpell);

            foreach (ulong key in TrackedFiles[0x9F]) {
                STUMapHeader map = GetInstance<STUMapHeader>(key);
                if (map == null) continue;
                MapHeader mapInfo = ListMaps.GetMap(key);
                mapInfo.Name = mapInfo.Name ?? "Title Screen";

                Dictionary<string, ParsedArg> config = GetQuery(parsedTypes, mapInfo.Name, mapInfo.VariantName,
                                                                mapInfo.GetUniqueName(), mapInfo.GetName(), teResourceGUID.Index(map.m_map).ToString("X"), "*");

                if (config.Count == 0) continue;

                Map.Save(flags, mapInfo, map, key, flags.OutputPath);
                SaveScratchDatabase();
            }
        }

        public List<QueryType> QueryTypes => new List<QueryType>();

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public static readonly Dictionary<string, string> MapMapping = new Dictionary<string, string> {
            ["horizon"] = "horizon lunar colony",
            ["moon"] = "horizon lunar colony",
            ["hlc"] = "horizon lunar colony",
            ["anubis"] = "temple of anubis",
            ["gibraltar"] = "watchpoint: gibraltar",
            ["watchpoint"] = "watchpoint: gibraltar",
            ["watchpoint gibraltar"] = "watchpoint: gibraltar",
            ["lijiang"] = "lijiang tower",
            ["estadio das ras"] = "estádio das rãs",
            ["chateau guillard"] = "château guillard",
            ["chateau"] = "château guillard",
            ["ecopoint"] = "ecopoint: antarctica",
            ["antarctica"] = "ecopoint: antarctica",
            ["ecopoint antarctica"] = "ecopoint: antarctica",
            ["volskaya"] = "volskaya industries",
            ["kings row"] = "king's row",
            ["paraiso"] = "paraíso",
            ["rio"] = "paraíso",
            ["esperanca"] = "esperança",
            ["portugal"] = "esperança",
        };

        public Dictionary<string, string> QueryNameOverrides => MapMapping;
    }
}
