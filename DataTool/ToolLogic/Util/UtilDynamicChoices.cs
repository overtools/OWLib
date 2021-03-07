using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using DataTool.ToolLogic.List;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;

namespace DataTool.ToolLogic.Util {
    [Tool("util-dynamic-choices", Description = "Export dynamic query choices", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class UtilDynamicChoices : JSONTool, ITool {
        public class DynamicChoicesContainer {
            public Dictionary<string, DynamicChoiceType> Types = new Dictionary<string, DynamicChoiceType>();

            public DynamicChoiceType GetType(string key) {
                if (Types.TryGetValue(key, out var container)) return container;
                container = new DynamicChoiceType();
                Types[key] = container;

                return container;
            }
        }

        public const string VALID_HERO_NAMES = "datatool.ux.valid_hero_names";
        public const string VALID_NPC_NAMES = "datatool.ux.valid_npc_names";
        public const string VALID_MAP_NAMES = "datatool.ux.valid_map_names";

        public const string VALID_SKIN_NAMES = "datatool.ux.valid_skin_names";
        public const string VALID_ICON_NAMES = "datatool.ux.valid_icon_names";
        public const string VALID_SPRAY_NAMES = "datatool.ux.valid_spray_names";
        public const string VALID_VICTORYPOSE_NAMES = "datatool.ux.valid_victorypose_names";
        public const string VALID_HIGHLIGHTINTRO_NAMES = "datatool.ux.valid_highlightintro_names";
        public const string VALID_EMOTE_NAMES = "datatool.ux.valid_emote_names";
        public const string VALID_VOICELINE_NAMES = "datatool.ux.valid_voiceline_names";

        public const string VALID_OWL_TEAMS = "datatool.ux.valid_owl_teams";

        public class DynamicChoiceType {
            public List<DynamicChoice> Choices = new List<DynamicChoice>();
        }

        public class DynamicChoice {
            public string QueryName;
            public string DisplayName;

            public DynamicChoicesContainer Children;
        }

        private static void ProcessHeroNames(DynamicChoicesContainer container) {
            var heroContainer = container.GetType(VALID_HERO_NAMES);
            var npcContainer = container.GetType(VALID_NPC_NAMES);
            var owlTeams = container.GetType(VALID_OWL_TEAMS);

            HashSet<string> handledTeams = new HashSet<string>();

            Dictionary<ulong, STUHero> heroes = new Dictionary<ulong, STUHero>();
            Dictionary<string, int> nameOccurrances = new Dictionary<string, int>();

            foreach (ulong heroGUID in Program.TrackedFiles[0x75]) {
                var hero = STUHelper.GetInstance<STUHero>(heroGUID);
                if (hero == null) continue;

                string heroNameActual = Hero.GetCleanName(hero);
                if (heroNameActual == null) continue;

                heroes[heroGUID] = hero;
                if (nameOccurrances.TryGetValue(heroNameActual, out _)) {
                    nameOccurrances[heroNameActual]++;
                } else {
                    nameOccurrances[heroNameActual] = 0;
                }
            }

            foreach (ulong heroGUID in Program.TrackedFiles[0x75]) {
                if (!heroes.TryGetValue(heroGUID, out var hero)) continue;
                string heroNameActual = Hero.GetCleanName(hero);

                var doGuidName = nameOccurrances[heroNameActual] > 1;
                if (doGuidName) {
                    heroNameActual += $" ({teResourceGUID.Index(heroGUID):X})";
                }

                ProgressionUnlocks progressionUnlocks = new ProgressionUnlocks(hero);

                var choice = new DynamicChoice {
                    DisplayName = heroNameActual,
                    QueryName = $"{teResourceGUID.Index(heroGUID):X}"
                };

                if (progressionUnlocks.LevelUnlocks == null) {
                    npcContainer.Choices.Add(choice);
                } else {
                    heroContainer.Choices.Add(choice);
                }

                foreach (var unlock in progressionUnlocks.IterateUnlocks()) {
                    if (string.IsNullOrWhiteSpace(unlock.Name)) continue;

                    if (unlock.STU.m_0B1BA7C1 != null) {
                        TeamDefinition teamDef = new TeamDefinition(unlock.STU.m_0B1BA7C1);
                        if (!(teamDef.Division == Enum_5A789F71.None && teamDef.Location == null)) {
                            if (handledTeams.Add(teamDef.FullName)) {
                                owlTeams.Choices.Add(new DynamicChoice {
                                    DisplayName = teamDef.FullName,
                                    QueryName = teamDef.FullName
                                });
                            }

                            continue;
                        }
                    }

                    var key = "datatool.ux.valid_" + unlock.Type.ToString().ToLowerInvariant() + "_names";

                    if (choice.Children == null) {
                        choice.Children = new DynamicChoicesContainer();
                    }

                    var test = choice.Children.GetType(key);
                    test.Choices.Add(new DynamicChoice {
                        QueryName = unlock.Name,
                        DisplayName = unlock.Name
                    });
                }
            }
        }

        private static void ProcessMapNames(DynamicChoicesContainer container) {
            var mapContainer = container.GetType(VALID_MAP_NAMES);

            foreach (ulong key in Program.TrackedFiles[0x9F]) {
                MapHeader map = ListMaps.GetMap(key);
                if (map == null) continue;

                var name = map.VariantName ?? map.Name ?? $"Title Screen ({teResourceGUID.Index(key):X})";

                mapContainer.Choices.Add(new DynamicChoice {
                    DisplayName = name,
                    QueryName = teResourceGUID.Index(map.MapGUID).ToString("X")
                });
            }
        }

        public void Parse(ICLIFlags toolFlags) {
            var info = new DynamicChoicesContainer();

            ProcessHeroNames(info);
            ProcessMapNames(info);

            OutputJSON(info, (ListFlags) toolFlags);
        }
    }
}
