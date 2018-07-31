using System;
using System.Collections.Generic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using Newtonsoft.Json;
using TankLib;
using TankLib.STU.Types;
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
            Dictionary<string, MapInfo> maps = GetMaps();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(maps, flags);
                    return;
                }

            var iD = new IndentHelper();
            foreach (KeyValuePair<string, MapInfo> map in maps) {
                var data = map.Value;

                Log($"{iD}{data.Name ?? map.Key} ({map.Value.MetadataGUID:X8})");

                if (!string.IsNullOrEmpty(data.NameB)) Log($"{iD+1}Alt Name: {data.NameB}");

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

        public static MapInfo GetMap(ulong key) {
            STUMapHeader map = GetInstance<STUMapHeader>(key);
            if (map == null) return null;

            string nameA = GetString(map.m_displayName);
            string nameB = GetString(map.m_1C706502);
            string subline = GetString(map.m_EBCFAD22);

            string descA = GetString(map.m_389CB894);
            string descB = GetString(map.m_ACB95597);

            string stateA = GetString(map.m_5AFE2F61);
            string stateB = GetString(map.m_8EBADA44);

            List<GamemodeInfo> gamemodes = null;
            
            if (map.m_D608E9F3 != null) {
                gamemodes = new List<GamemodeInfo>();
                foreach (ulong guid in map.m_D608E9F3) {
                    STUGameMode gamemode = GetInstance<STUGameMode>(guid);
                    if (gamemode == null) {
                        continue;
                    }
                        
                    gamemodes.Add(new GamemodeInfo {
                        Name = GetString(gamemode.m_displayName),
                        GUID = guid
                    });
                }
            } 
            
            // if (map.m_celebrationOverrides != null) {
            //     
            //
            // }

            if (!string.IsNullOrEmpty(nameA) && !string.IsNullOrEmpty(nameB) && nameB.Equals(nameA)) {
                nameB = null;
            }

            if (!string.IsNullOrEmpty(descA) && !string.IsNullOrEmpty(descB) && descB.Equals(descA)) {
                descB = null;
            }

            string uniqueName;
            if (nameA == null) {
                uniqueName = $"Title Screen:{teResourceGUID.Index(key):X}";
            } else {
                uniqueName = nameA + $":{teResourceGUID.Index(key):X}";
            }
            return new MapInfo(uniqueName, key, nameA, nameB, descA, descB, subline, stateA, stateB,
                map.m_map, map.m_baseMap, gamemodes);
        }

        public Dictionary<string, MapInfo> GetMaps() {
            Dictionary<string, MapInfo> @return = new Dictionary<string, MapInfo>();

            foreach (ulong key in TrackedFiles[0x9F]) {
                MapInfo map = GetMap(key);
                if (map == null) continue;
                @return[map.UniqueName] = map;
            }

            return @return;
        }
    }
}