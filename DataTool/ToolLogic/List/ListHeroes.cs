using System;
using System.Collections.Generic;
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
    public class ListHeroes : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class HeroInfo {
            public string Name;
            public string Description;
            public string Color;
            public Dictionary<string, AbilityInfo> Abilities;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong GUID;
            
            public HeroInfo(ulong guid, string name, string description, string color, Dictionary<string, AbilityInfo> abilities) {
                GUID = guid;
                Name = name;
                Description = description;
                Color = color;
                Abilities = abilities;
            }
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class AbilityInfo {
            public string Name;
            public string Type;
            public string Description;
            
            [JsonConverter(typeof(GUIDConverter))]
            public ulong GUID;

            public AbilityInfo() {}

            public AbilityInfoInner ToInner() {
                return new AbilityInfoInner(GUID, Name, new List<string> {Description}, Type);
            }
            
            public AbilityInfo(ulong guid, string name, string description, string type) {
                GUID = guid;
                Name = name;
                Type = type;
                Description = description;
            }
        }

        public class AbilityInfoInner : AbilityInfo {
            public List<string> Descriptions;
            
            public AbilityInfoInner(ulong guid, string name, List<string> descriptions, string type) {
                GUID = guid;
                Name = name;
                Type = type;
                Descriptions = descriptions;
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
                if (hero.Value.Description != null)
                    Log($"{indentLevel + 1}Description: {hero.Value.Description}");
                
                if (hero.Value.Color != null)
                    Log($"{indentLevel + 1}Color: {hero.Value.Color}");

                if (hero.Value.Abilities != null) {
                    Dictionary<string, AbilityInfoInner> abilities = new Dictionary<string, AbilityInfoInner>();

                    foreach (KeyValuePair<string, AbilityInfo> ability in hero.Value.Abilities) {
                        if (!abilities.ContainsKey(ability.Value.Name)) {
                            abilities[ability.Value.Name] = ability.Value.ToInner();
                        } else {
                            abilities[ability.Value.Name].Descriptions.Add(ability.Value.Description);
                        }
                    }

                    Log($"{indentLevel + 1}Abilities:");
                    foreach (KeyValuePair<string, AbilityInfoInner> ability in abilities) {
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
            STULoadout ability = GetInstance<STULoadout>(key);
            if (ability == null) return null;

            return new AbilityInfo(key, GetString(ability.Name), GetString(ability.Description), ability.Category.ToString());
        }

        public Dictionary<string, AbilityInfo> GetAbilities(STUHero hero) {
            Dictionary<string, AbilityInfo> @return = new Dictionary<string, AbilityInfo>();

            if (hero.Abilities == null) {
                return null;
            }

            foreach (Common.STUGUID ability in hero.Abilities) {
                AbilityInfo abi = GetAbility(ability);

                string name = abi.Name == null ? $"Unknown{GUID.Index(ability):X}" : $"{abi.Name}:{GUID.Index(ability):X}";               
                @return[name] = abi;
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
                string color = null;

                if (hero.GalleryColor != null)
                    color = hero.GalleryColor.Hex();

                @return[name] = new HeroInfo(key, name, description, color, GetAbilities(hero));
            }

            return @return;
        }
    }
}