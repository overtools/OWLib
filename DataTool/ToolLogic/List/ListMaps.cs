using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using Newtonsoft.Json;
using OWLib;
using STULib.Types;
using STULib.Types.Generic;
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
            public string UniqueName;
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

            public MapInfo(string uniqueName, ulong dataGUID, string name, string nameB, string description, string descriptionB,
                string subline, string stateA, string stateB, ulong mapData1, ulong mapData2, List<GamemodeInfo> gamemodes) {
                UniqueName = uniqueName;
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
            List<MapInfo> maps = GetMaps();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(maps, flags);
                    return;
                }

            var iD = new IndentHelper();
            foreach (var map in maps) {

                Log($"{iD}{map.Name ?? map.UniqueName}");
                Log($"{iD+1}Unique Name: {map.UniqueName}");
                Log($"{iD+1}Key: {GUID.Index(map.MetadataGUID):X}");
                
                if (!string.IsNullOrEmpty(map.NameB)) Log($"{iD+1}Alt Name: {map.NameB}");

                if (!string.IsNullOrEmpty(map.Description)) Log($"{iD+1}Desc: {map.Description}");
                if (!string.IsNullOrEmpty(map.DescriptionB)) Log($"{iD+1}DescB: {map.DescriptionB}");
                if (!string.IsNullOrEmpty(map.Subline)) Log($"{iD+1}Subline: {map.Subline}");

                if (map.StateA != null || map.StateB != null) {
                    Log($"{iD+1}States:");
                    Log($"{iD+2}{map.StateA}");
                    Log($"{iD+2}{map.StateB}");
                }

                if (map.Gamemodes != null) {
                    Log($"{iD+1}Gamemodes:"); 
                    map.Gamemodes.ForEach(m => Log($"{iD+2}{m.Name}"));
                }

                Log();
            }
        }

        public static MapInfo GetMap(ulong key) {
            STUMap map = GetInstance<STUMap>(key);
            if (map == null) return null;

            string nameA = GetString(map.DisplayName);
            string nameB = GetString(map.VariantName);

            string descA = GetString(map.DescriptionA);
            string descB = GetString(map.DescriptionB);
            string subline = GetString(map.SublineString);

            string stateA = GetString(map.StateStringA);
            string stateB = GetString(map.StateStringB);

            List<GamemodeInfo> gamemodes = null;
            if (map.Gamemodes != null) {
                gamemodes = new List<GamemodeInfo>();
                foreach (Common.STUGUID guid in map.Gamemodes) {
                    STUGamemode gamemode = GetInstance<STUGamemode>(guid);
                    if (gamemode == null) {
                        continue;
                    }
                        
                    gamemodes.Add(new GamemodeInfo {
                        Name = GetString(gamemode.DisplayName),
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
                uniqueName = $"Title Screen:{GUID.Index(key):X}";
            } else {
                uniqueName = nameA + $":{GUID.Index(key):X}";
            }
            return new MapInfo(uniqueName, key, nameA, nameB, descA, descB, subline, stateA, stateB,
                map.MapDataResource1, map.MapDataResource2, gamemodes);
        }

        public List<MapInfo> GetMaps() {
            List<MapInfo> @return = new List<MapInfo>();

            foreach (ulong key in TrackedFiles[0x9F]) {
                MapInfo map = GetMap(key);
                if (map == null) continue;
                
                @return.Add(map);
            }

            return @return.OrderBy(m => m.MetadataGUID).ToList();
        }
    }
}