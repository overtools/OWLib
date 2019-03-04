using System;
using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.DataModels.GameModes;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-maps", Description = "List maps", CustomFlags = typeof(ListFlags))]
    public class ListMaps : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            Dictionary<teResourceGUID, MapHeader> maps = GetMaps();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(maps, flags);
                    return;
                }

            var iD = new IndentHelper();
            foreach (var map in maps) {
                var data = map.Value;

                Log($"{iD}{data.GetUniqueName()} ({data.MapGUID:X8})");

                if (!string.IsNullOrEmpty(data.Name)) Log($"{iD+1}Name: {data.Name}");
                if (!string.IsNullOrEmpty(data.VariantName)) Log($"{iD+1}VariantName: {data.VariantName}");
                if (!string.IsNullOrEmpty(data.Description)) Log($"{iD+1}Desc1: {data.Description}");
                if (!string.IsNullOrEmpty(data.Description2)) Log($"{iD+1}Desc2: {data.Description2}");
                Log($"{iD+1}Status: {data.State}");
                Log($"{iD+1}Type: {data.MapType}");

                // if (!string.IsNullOrEmpty(data.Description)) Log($"{iD+1}Desc: {data.Description}");
                // if (!string.IsNullOrEmpty(data.DescriptionB)) Log($"{iD+1}DescB: {data.DescriptionB}");
                // if (!string.IsNullOrEmpty(data.Subline)) Log($"{iD+1}Subline: {data.Subline}");
                //
                // if (data.StateA != null || data.StateB != null) {
                //     Log($"{iD+1}States:");
                //     Log($"{iD+2}{data.StateA}");
                //     Log($"{iD+2}{data.StateB}");
                // }

                if (data.GameModes != null) {
                    Log($"{iD+1}GameModes:");

                    foreach (teResourceGUID mode in data.GameModes) {
                        var stu = GetInstance<STUGameMode>(mode);
                        if (stu == null) continue;
                        GameMode gameMode = new GameMode(stu);
                        Console.Out.WriteLine($"{iD+2}{gameMode.Name}");
                    }
                    //data.GameModes.ForEach(m => Log($"{iD+2}{m.Name}"));
                }

                Log();
            }
        }

        public static MapHeader GetMap(ulong key) {
            STUMapHeader map = GetInstance<STUMapHeader>(key);
            if (map == null) return null;

            return new MapHeader(map);
        }

        public Dictionary<teResourceGUID, MapHeader> GetMaps() {
            Dictionary<teResourceGUID, MapHeader> @return = new Dictionary<teResourceGUID, MapHeader>();

            foreach (teResourceGUID key in TrackedFiles[0x9F]) {
                MapHeader map = GetMap(key);
                if (map == null) continue;
                @return[key] = map;
            }

            return @return;
        }
    }
}