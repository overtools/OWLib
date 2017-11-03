using System;
using System.Collections.Generic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using Newtonsoft.Json;
using OWLib;
using STULib.Types;
using STULib.Types.Gamemodes;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-maps", Description = "List maps", TrackTypes = new ushort[] {0x9F}, CustomFlags = typeof(ListFlags))]
    public class ListMaps : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public class GamemodeInfo {
            public string Name;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong GUID;
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class MapInfo {
            public string Name;
            public string NameB;
            public string Description;
            public string DescriptionB;
            public string Subline;
            public string StateA;
            public string StateB;

            public List<GamemodeInfo> Gamemodes;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong MetadataGUID;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong DataGUID1;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong DataGUID2;

            public MapInfo(ulong dataGUID, string name, string nameB, string description, string descriptionB,
                string subline, string stateA, string stateB, ulong mapData1, ulong mapData2, List<GamemodeInfo> gamemodes) {
                MetadataGUID = dataGUID;
                Name = name;
                NameB = nameB;
                Description = description;
                DescriptionB = descriptionB;
                Subline = subline;
                StateA = stateA;
                StateB = stateB;
                DataGUID1 = mapData1;
                DataGUID2 = mapData2;
                Gamemodes = gamemodes;
            }
        }

        public void Parse(ICLIFlags toolFlags) {
            Dictionary<string, MapInfo> maps = GetMaps(false);

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(maps, flags);
                    return;
                }

            var iD = new IndentHelper();
            var i = 0;
            foreach (KeyValuePair<string, MapInfo> map in maps) {

                Log($"[{i}]");
                i++;

                var data = map.Value;

                Log($"{iD+1}Name: {data.Name ?? map.Key}");

                if (!string.IsNullOrEmpty(data.NameB)) Log($"{iD+1}NameB: {data.NameB}");

                if (!string.IsNullOrEmpty(data.Description)) Log($"{iD+1}Desc: {data.Description}");
                if (!string.IsNullOrEmpty(data.DescriptionB)) Log($"{iD+1}DescB: {data.DescriptionB}");
                if (!string.IsNullOrEmpty(data.Subline)) Log($"{iD+1}Subline: {data.Subline}");

                if (data.StateA != null || data.StateB != null) {
                    Log($"{iD+1}States:");
                    Log($"{iD+2}{data.StateA}");
                    Log($"{iD+2}{data.StateB}");
                }

                if (data.Gamemodes != null) {
                    Log($"{iD+1}Gamemodes:");
                    data.Gamemodes.ForEach(m => Log($"{iD+2}{m.Name}"));
                }

                Log();
            }
        }

        public Dictionary<string, MapInfo> GetMaps(bool ignoreUnknown) {
            Dictionary<string, MapInfo> @return = new Dictionary<string, MapInfo>();

            foreach (ulong key in TrackedFiles[0x9F]) {
                var map = GetInstance<STUMap>(key);
                if (map == null) continue;

                var nameA = GetString(map.Name);
                var nameB = GetString(map.NameB);

                var descA = GetString(map.DescriptionA);
                var descB = GetString(map.DescriptionB);
                var subline = GetString(map.SublineString);

                var stateA = GetString(map.StateStringA);
                var stateB = GetString(map.StateStringB);

                List<GamemodeInfo> gamemodes = null;
                if (map.Gamemodes != null) {
                    gamemodes = new List<GamemodeInfo>();
                    foreach (var guid in map.Gamemodes) {
                        var gamemode = GetInstance<STUGamemode>(guid);
                        if (gamemode == null) {
                            continue;
                        }
                        
                        gamemodes.Add(new GamemodeInfo {
                            Name = GetString(gamemode.Name),
                            GUID = guid
                        });
                    }

                }

                if (!string.IsNullOrEmpty(nameA) && !string.IsNullOrEmpty(nameB) && nameB.Equals(nameA)) {
                    nameB = null;
                }

                if (!string.IsNullOrEmpty(descA) && !string.IsNullOrEmpty(descB) && descB.Equals(descA)) {
                    descB = null;
                }

                string uniqueName;
                if (nameA == null) {
                    if (ignoreUnknown) continue;
                    uniqueName = $"Unknown{GUID.Index(key):X}";
                } else {
                    uniqueName = nameA + $":{GUID.Index(key):X}";
                }

                @return[uniqueName] = new MapInfo(key, nameA, nameB, descA, descB, subline, stateA, stateB,
                    map.MapDataResource1, map.MapDataResource2, gamemodes);
            }

            return @return;
        }
    }
}