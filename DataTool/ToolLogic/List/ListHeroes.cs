using System;
using System.Collections.Generic;
using System.IO;
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
    [Tool("list-heroes", Description = "List heroes", TrackTypes = new ushort[] {0x75}, CustomFlags = typeof(ListFlags))]
    public class ListHeroes : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class HeroInfo {
            public string Name;
            public string Description;
            public Dictionary<string, AbilityInfo> Abilities;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong GUID;
            
            public HeroInfo(ulong guid, string name, string description, Dictionary<string, AbilityInfo> abilities) {
                GUID = guid;
                Name = name;
                Description = description;
                Abilities = abilities;
            }
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class AbilityInfo {
            public string Name;
            public string Type;
            public string[] Descriptions;
            
            [JsonConverter(typeof(GUIDArrayConverter))]
            public ulong[] GUIDs;
            
            public AbilityInfo(ulong[] guid, string name, string[] descriptions, string type) {
                GUIDs = guid;
                Name = name;
                Type = type;
                Descriptions = descriptions;
            }
        }

        internal void ParseJSON(Dictionary<string, HeroInfo> heroes, ListFlags toolFlags) {
            string json = JsonConvert.SerializeObject(heroes, Formatting.Indented);
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
            Dictionary<string, HeroInfo> heroes = GetHeroes();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(heroes, flags);
                    return;
                }

            IndentHelper indentLevel = new IndentHelper();
            
            foreach (KeyValuePair<string, HeroInfo> hero in heroes) {
                Log($"{hero.Value.Name}");
                if (hero.Value.Description != null) {
                    Log($"{indentLevel + 1}Description: {hero.Value.Description}");
                }

                if (hero.Value.Abilities != null) {
                    Log($"{indentLevel + 1}Abilities:");
                    foreach (KeyValuePair<string,AbilityInfo> ability in hero.Value.Abilities) {
                        Log($"{indentLevel + 2}{ability.Value.Name}: {ability.Value.Type}");
                        foreach (string description in ability.Value.Descriptions) {
                            if (description == null) continue;
                            Log($"{indentLevel + 3}{description}");
                        }
                    }
                }

                Log();
            }
        }

        public AbilityInfo GetAbility(Common.STUGUID key) {
            STUAbilityInfo ability = GetInstance<STUAbilityInfo>(key);
            if (ability == null) return null;

            return new AbilityInfo(new[] {(ulong) key}, GetString(ability.Name), new[] {GetString(ability.Description)},
                ability.AbilityType.ToString());
        }

        public Dictionary<string, AbilityInfo> GetAbilities(STUHero hero) {
            Dictionary<string, AbilityInfo> @return = new Dictionary<string, AbilityInfo>();

            if (hero.Abilities == null) {
                return null;
            }

            foreach (Common.STUGUID ability in hero.Abilities) {
                AbilityInfo abi = GetAbility(ability);

                string name = abi.Name ?? $"Unknown{GUID.Index(ability):X}";
                
                if (@return.ContainsKey(name)) {
                    List<string> descrList = @return[name].Descriptions.ToList();
                    descrList.AddRange(abi.Descriptions);
                    @return[name].Descriptions = descrList.ToArray();
                    
                    List<ulong> guidList = @return[name].GUIDs.ToList();
                    guidList.AddRange(abi.GUIDs);
                    @return[name].GUIDs = guidList.ToArray();
                } else {
                    @return[name] = abi;
                }
                
            }
            
            return @return;
        }

        public Dictionary<string, HeroInfo> GetHeroes() {
            Dictionary<string, HeroInfo> @return = new Dictionary<string, HeroInfo>();

            foreach (ulong key in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(key);
                if (hero == null) continue;

                string name = GetString(hero.Name) ?? $"Unknown{GUID.Index(key):X}";

                string description = GetDescriptionString(hero.Description);

                @return[name] = new HeroInfo(key, name, description, GetAbilities(hero));
            }

            return @return;
        }
    }
}