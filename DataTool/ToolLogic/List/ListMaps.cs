using System;
using System.Collections.Generic;
using System.IO;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using Newtonsoft.Json;
using OWLib;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-maps", Description = "List maps", TrackTypes = new ushort[] {0x9F}, CustomFlags = typeof(ListFlags))]
    public class ListMaps : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class MapInfo {
            public string Name;
            public string NameB;
            public string Description;
            public string DescriptionB;
            public string StateA;
            public string StateB;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong MetadataGUID;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong DataGUID1;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong DataGUID2;

            public MapInfo(ulong dataGUID, string name, string nameB, string description, string descriptionB,
                string stateA, string stateB, ulong mapData1, ulong mapData2) {
                MetadataGUID = dataGUID;
                Name = name;
                NameB = nameB;
                Description = description;
                DescriptionB = descriptionB;
                StateA = stateA;
                StateB = stateB;
                DataGUID1 = mapData1;
                DataGUID2 = mapData2;
            }
        }

        internal void ParseJSON(Dictionary<string, MapInfo> maps, ListFlags toolFlags) {
            string json = JsonConvert.SerializeObject(maps, Formatting.Indented);
            if (!string.IsNullOrWhiteSpace(toolFlags.Output)) {
                Log("Writing to {0}", toolFlags.Output);

                CreateDirectoryFromFile(toolFlags.Output);

                using (Stream file = File.OpenWrite(toolFlags.Output)) {
                    using (TextWriter writer = new StreamWriter(file)) {
                        writer.WriteLine(json);
                    }
                }
            } else {
                Console.Error.WriteLine(json);
            }
        }

        public void Parse(ICLIFlags toolFlags) {
            Dictionary<string, MapInfo> maps = GetMaps(false);

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(maps, flags);
                    return;
                }

            IndentHelper indentLevel = new IndentHelper();
            foreach (KeyValuePair<string, MapInfo> map in maps) {
                Log(map.Value.NameB ?? map.Value.Name);

                if (map.Value.Description != null) Log($"{indentLevel + 1}Description: {map.Value.Description}");
                if (map.Value.DescriptionB != null && map.Value.DescriptionB != map.Value.Description)
                    Log($"{indentLevel + 1}DescriptionB: {map.Value.DescriptionB}");

                if (map.Value.StateA != null || map.Value.StateB != null) {
                    Log($"{indentLevel + 1}States:");
                    Log($"{indentLevel + 2}{map.Value.StateA}");
                    Log($"{indentLevel + 2}{map.Value.StateB}");
                }

                Log();
            }
        }

        public Dictionary<string, MapInfo> GetMaps(bool ignoreUnknown) {
            Dictionary<string, MapInfo> @return = new Dictionary<string, MapInfo>();

            foreach (ulong key in TrackedFiles[0x9F]) {
                STUMap map = GetInstance<STUMap>(key);
                if (map == null) continue;

                string name = GetString(map.Name);
                string nameB = GetString(map.NameB);

                string uniqueName;
                if (name == null) {
                    if (ignoreUnknown) continue;
                    uniqueName = $"Unknown{GUID.Index(key):X}";
                } else {
                    uniqueName = name + $":{GUID.Index(key):X}";
                }

                string description = GetString(map.DescriptionA);
                string description2 = GetString(map.DescriptionB);

                string stateA = GetString(map.StateStringA);
                string stateB = GetString(map.StateStringB);
                // string subline = GetString(map.SublineString);

                @return[uniqueName] = new MapInfo(key, name, nameB, description, description2, stateA, stateB,
                    map.MapDataResource1, map.MapDataResource2);
            }


            return @return;
        }
    }
}