using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.DataModels.GameModes;
using DataTool.DataModels.Hero;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.Helper {
    public class CriteriaContext {
        private readonly Dictionary<ulong, string> m_heroes = [];
        private readonly Dictionary<ulong, string> m_maps = [];
        private readonly Dictionary<ulong, string> m_gameModes = [];
        private readonly Dictionary<ulong, string> m_missions = [];
        private readonly Dictionary<ulong, string> m_objectives = [];
        private readonly Dictionary<ulong, string> m_talents = [];

        public string GetHeroName(ulong guid) {
            if (m_heroes.TryGetValue(guid, out var name)) {
                return name;
            }

            // todo: this loads a lot of non-required things
            var hero = Hero.Load(guid);
            name = hero?.Name ?? GetNoTypeGUIDName(guid);
            
            m_heroes.Add(guid, name);
            return name;
        }
        
        public string GetMapName(ulong guid) {
            if (m_maps.TryGetValue(guid, out var name)) {
                return name;
            }

            // todo: this loads a lot of non-required things
            var map = MapHeader.LoadFromMap(guid);
            name = map?.GetName() ?? GetNoTypeGUIDName(guid);
            
            m_maps.Add(guid, name);
            return name;
        }

        public string GetGameModeName(ulong guid) {
            if (m_gameModes.TryGetValue(guid, out var name)) {
                return name;
            }
            
            // todo: this loads a lot of non-required things
            var gameMode = GameMode.Load(guid);
            name = gameMode?.Name ?? GetNoTypeGUIDName(guid);
            
            m_gameModes.Add(guid, name);
            return name;
        }

        public string GetMissionName(ulong guid) {
            if (m_missions.TryGetValue(guid, out var name)) {
                return name;
            }
            
            var mission = GetInstance<STU_8B0E97DC>(guid);
            name = GetCleanString(mission?.m_0EDCE350) ?? GetNoTypeGUIDName(guid);
            
            m_missions.Add(guid, name);
            return name;
        }

        public string GetObjectiveName(ulong guid) {
            if (m_objectives.TryGetValue(guid, out var name)) {
                return name;
            }
            
            // todo: these are not the user-facing names...
            var objective = GetInstance<STU_19A98AF4>(guid);
            name = GetCleanString(objective?.m_name) ?? GetNoTypeGUIDName(guid);
            
            m_objectives.Add(guid, name);
            return name;
        }
        
        public string GetTalentName(ulong guid) {
            if (m_talents.TryGetValue(guid, out var name)) {
                return name;
            }
            
            // todo: cache
            var talent = Talent.Load(guid);
            name = talent?.Name  ?? GetNoTypeGUIDName(guid);
            
            m_talents.Add(guid, name);
            return name;
        }
        
        public string GetCelebrationName(ulong guid) 
        {
            return GetNoTypeGUIDName(guid);
        }

        public string GetTagName(ulong guid) 
        {
            return GetNoTypeGUIDName(guid);
        }

        private static string GetNoTypeGUIDName(ulong guid) {
            var manualName = GetNullableGUIDName(guid);
            if (manualName != null) return manualName;

            return $"Unknown{teResourceGUID.Index(guid):X}";
        }

        public void BuildCriteriaDescription(IndentedTextWriter writer, STUCriteriaContainer container) {
            if (container is not STU_32A19631 embedCriteria) {
                // I don't think the asset ref version is used
                // probably deleted by build pipeline
                writer.WriteLine("Error: non-embedded criteria");
                return;
            }

            BuildCriteriaDescription(writer, embedCriteria.m_criteria);
        }

        private void BuildCriteriaDescription(IndentedTextWriter writer, STUCriteria criteria) {
            switch (criteria) {
                case null:
                    writer.WriteLine("Error: null criteria");
                    break;
                case STUCriteria_Statescript statescript: {
                    using var _ = new ModifierScope(writer, statescript.m_57D96E27, "NOT");
                    
                    writer.Write($"Scripted Event: {teResourceGUID.Index(statescript.m_identifier):X}");
                    break;
                }
                case STU_D815520F heroInteraction: {
                    using var _ = new ModifierScope(writer, heroInteraction.m_57D96E27, "NOT");
                    
                    // kill lines
                    writer.Write($"Hero Interaction: {GetHeroName(heroInteraction.m_8C8C5285)}");
                    break;
                }
                case STUCriteria_IsHero isHero:
                    // also kill lines...
                    // not clear what the difference is
                    writer.Write($"Is Hero: {GetHeroName(isHero.m_hero)}");
                    break;
                
                case STU_3EAADDE8 teamInteraction: {
                    // e.g "Look at us! The full might of Overwatch, reassembled and ready to rumble!"
                    using var _ = new ModifierScope(writer, teamInteraction.m_990CFF1C, "NOT");
                    
                    if (teamInteraction.m_hero != 0) {
                        writer.Write($"Hero On Team: {GetHeroName(teamInteraction.m_hero)}");
                    } else {
                        writer.Write($"Tag On Teammate: {GetTagName(teamInteraction.m_7D7C86A1)}");
                    }
                    break;
                }
                case STUCriteria_Team team:
                    writer.WriteLine($"On Team Number: {team.m_team}. UnkBool: {team.m_EB5492C4 != 0}");
                    break;
                
                case STU_A95E4B99 gender:
                    // specializing lines for different pronouns
                    writer.WriteLine($"Required Gender: {gender.m_gender}");
                    break;
                case STU_C9F4617F gender2:
                    // same thing...
                    writer.WriteLine($"Required Gender: {gender2.m_gender}");
                    break;
                
                case STU_C37857A5 celebration:
                    writer.WriteLine($"Active Celebration: {GetCelebrationName(celebration.m_celebrationType)}");
                    break;
                
                case STUCriteria_OnMap onMap: 
                    writer.WriteLine($"On Map: {GetMapName(onMap.m_map)}. Allow Event Variants: {(onMap.m_exactMap != 0 ? "false" : "true")}");
                    break;
                case STU_4A7A3740 onGameMode: {
                    // NOT used by volleyball of all things, to override ult lines
                    using var _ = new ModifierScope(writer, onGameMode.m_E9A758B4, "NOT");
                    
                    writer.Write($"On Game Mode: {GetGameModeName(onGameMode.m_gameMode)}");
                    break;
                }
                case STU_0F78DDB0 onMission: {
                    using var _ = new ModifierScope(writer, onMission.m_89B967D3, "NOT");
                    
                    writer.Write($"On Mission: {GetMissionName(onMission.m_216EA6DA)}");
                    break;
                }
                case STU_20ABB515 onObjective:
                    // usually used with a mission criteria, even though objectives are mission specific
                    writer.WriteLine($"On Mission Objective: {GetObjectiveName(onObjective.m_4992CB75)}");
                    break;
                
                case STU_31297254 hasTalent: {
                    // todo: NOT is unused, might be wrong
                    using var _ = new ModifierScope(writer, hasTalent.m_8F034FB5, "NOT");
                    
                    writer.Write($"Has Talent: {GetTalentName(hasTalent.m_91A9D4CC)}");
                    break;
                }
                
                case STU_A9B89EC9 pve3:
                    // used for wotb
                    // Bet you I find the next key!
                    //    STU_A9B89EC9: 000000008253.01C
                    writer.WriteLine($"STU_A9B89EC9: {pve3.m_98D3EC50?.m_id}");
                    break;
                case STU_9665B416 unk9665B416:
                    writer.WriteLine($"STU_9665B416: {unk9665B416.m_EF135378?.m_id}");
                    break;
                case STU_E6EBD07B unkE6EBD07B:
                    writer.WriteLine($"STU_E6EBD07B: {unkE6EBD07B.m_E755B82A?.m_id}");
                    break;
                
                case STU_7C69EA0F nestedContainer: {
                    writer.WriteLine($"Nested - {nestedContainer.m_amount}/{nestedContainer.m_criteria.Length} Required:");
                    writer.Indent++;
                    foreach (var nested in nestedContainer.m_criteria) {
                        BuildCriteriaDescription(writer, nested);
                    }
                    writer.Indent--;
                    break;
                }
                default: {
                    writer.WriteLine($"Unknown: {criteria.GetType().Name}");
                    
                    // debug: exit on unknown found (you should turn off saving also)
                    // Console.Out.WriteLine(writer.InnerWriter.ToString());
                    // Environment.Exit(0);
                    break;
                }
            }
        }

        private ref struct ModifierScope : IDisposable {
            private readonly IndentedTextWriter m_writer;
            private readonly bool m_active;
            
            public ModifierScope(IndentedTextWriter writer, byte condition, string op) {
                // this would not work over an entire container for example... but doesn't seem needed
                // also the only operator seems to be NOT
                
                m_writer = writer;
                m_active = condition != 0;
                if (m_active) {
                    writer.Write($"{op} (");
                }
            }
            
            public void Dispose() {
                if (m_active) {
                    m_writer.Write(")");
                }
                m_writer.WriteLine();
            }
        }
    }
}
