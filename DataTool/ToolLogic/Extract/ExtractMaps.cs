using System;
using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.List;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.Logger;
using Map = DataTool.SaveLogic.Map;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-maps", Description = "Extract maps", TrackTypes = new ushort[] {0x9F, 0x0BC}, CustomFlags = typeof(ExtractFlags))]
    public class ExtractMaps : QueryParser, ITool, IQueryParser {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            SaveMaps(toolFlags);
        }
        
        protected override void QueryHelp(List<QueryType> types) {
            IndentHelper indent = new IndentHelper();
            Log("Please specify what you want to extract:");
            Log($"{indent+1}Command format: \"{{map name}}\" ");
            Log($"{indent+1}Each query should be surrounded by \", and individual queries should be separated by spaces");
            
            
            Log($"{indent+1}Maps can be listed using the \"list-maps\" mode");
            Log($"{indent+1}All map names are in your selected locale");
            
            Log("\r\nExample commands: ");
            Log($"{indent+1}\"Kings Row\"");
            Log($"{indent+1}\"Ilios\" \"Oasis\"");
        }

        public void SaveMaps(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            
            Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes = ParseQuery(flags, QueryTypes, QueryNameOverrides);
            if (parsedTypes == null) {QueryHelp(QueryTypes); return;}
            foreach (ulong key in TrackedFiles[0x9F]) {
                STUMapHeader map = GetInstance<STUMapHeader>(key);
                if (map == null) continue;
                MapHeader mapInfo = ListMaps.GetMap(key);
                mapInfo.Name = mapInfo.Name ?? "Title Screen";

                Dictionary<string, ParsedArg> config = GetQuery(parsedTypes, mapInfo.Name, mapInfo.VariantName,
                    mapInfo.GetUniqueName(), mapInfo.GetName(), teResourceGUID.Index(map.m_map).ToString("X"), "*");
                
                if (config.Count == 0) continue;
                
                Map.Save(flags, map, key, basePath);
            }
        }

        public List<QueryType> QueryTypes => new List<QueryType> {new QueryType {Name = "MapFakeType"}};
        public Dictionary<string, string> QueryNameOverrides => new Dictionary<string, string>();
    }
}