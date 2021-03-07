using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using DataTool.DataModels.GameModes;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.DataModels.GameModes.GameRulesetSchemaEntry;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-game-rulesets", Description = "List game rulesets", CustomFlags = typeof(ListFlags))]
    public class ListGameRulesets : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(data, flags);
                    return;
                }
        }

        private Dictionary<teResourceGUID, GameRuleset> GetData() {
            var @return = new Dictionary<teResourceGUID, GameRuleset>();

            // Hero rulesets that are used on Heroes
            var heroRulesetMapping = new Dictionary<teResourceGUID, RulesetSchemaMapping>();
            foreach (teResourceGUID key in TrackedFiles[0x75]) {
                var hero = STUHelper.GetInstance<STUHero>(key);
                if (hero?.m_gameRulesetSchemas == null) continue;

                foreach (var guid in hero.m_gameRulesetSchemas) {
                    var rulesetSchema = new GameRulesetSchema(guid);
                    if (rulesetSchema?.Entries == null) continue;

                    foreach (var rulesetSchemaEntry in rulesetSchema.Entries) {
                        heroRulesetMapping[rulesetSchemaEntry.Virtual01C] = new RulesetSchemaMapping {
                            GUID = rulesetSchemaEntry.Virtual01C,
                            Name = rulesetSchemaEntry.Name,
                            Value = rulesetSchemaEntry.Value
                        };
                    }
                }
            }

            foreach (teResourceGUID key in TrackedFiles[0xC0]) {
                var ruleset = new GameRuleset(key);
                if (ruleset.GUID == 0)
                    continue;

                // Generate game rulesets schemas that are used for this gamemode
                var rulesetMapping = new Dictionary<teResourceGUID, RulesetSchemaMapping>();
                foreach (var guid in ruleset.GameMode.GameMode.GameRulesetSchemas) {
                    var rulesetSchema = new GameRulesetSchema(guid);

                    foreach (var rulesetSchemaEntry in rulesetSchema?.Entries) {
                        rulesetMapping[rulesetSchemaEntry.Virtual01C] = new RulesetSchemaMapping {
                            GUID = rulesetSchemaEntry.Virtual01C,
                            Name = rulesetSchemaEntry.Name,
                            Value = rulesetSchemaEntry.Value
                        };
                    }
                }

                ruleset.GameMode.ConfigValues = ruleset.GameMode?.STU?.m_3CE93B76?
                    .Select(configValuePair => GenerateConfigValues(configValuePair, rulesetMapping))
                    .Where(x => x != null)
                    .ToArray();

                foreach (var gameRulesetTeam in ruleset.GameMode?.Teams) {
                    gameRulesetTeam.ConfigValues = gameRulesetTeam.STU.m_CF58324E?
                        .Select(configValuePair => GenerateConfigValues(configValuePair, heroRulesetMapping))
                        .Where(x => x != null)
                        .ToArray();
                }

                if (ruleset.GameMode.ConfigValues?.Length == 0) {
                    ruleset.GameMode.ConfigValues = null;
                }

                @return[key] = ruleset;
            }

            return @return;
        }

        private class RulesetSchemaMapping {
            public teResourceGUID GUID;
            public string Name;
            public RulesetSchemaValue Value;
        }

        private GameRulesetGameMode.GamemodeRulesetValue GenerateConfigValues(STUGameModeVarValuePair valuePair, Dictionary<teResourceGUID, RulesetSchemaMapping> rulesetMapping) {
            if (!rulesetMapping.ContainsKey(valuePair.m_3E783677))
                return new GameRulesetGameMode.GamemodeRulesetValue {
                    Virtual01C = valuePair.m_3E783677
                };

            var rulesetInfo = rulesetMapping[valuePair.m_3E783677];
            var configValue = new GameRulesetGameMode.GamemodeRulesetValue {
                Name = rulesetInfo.Name,
                Virtual01C = valuePair.m_3E783677
            };

            switch (valuePair.m_value) {
                case STU_22BF1D21 stu: // RulesetSchemaValueEnumChoice
                    configValue.Value = ((RulesetSchemaValueEnum) rulesetInfo.Value).Choices?.FirstOrDefault(x => x.Identifier == stu.m_value)?.DisplayText;
                    break;
                case STU_3ED2A372 stu: // RulesetSchemaValueInt
                    configValue.Value = stu.m_value.ToString();
                    break;
                case STU_73715F2E stu: // RulesetSchemaValueFloat
                    configValue.Value = stu.m_value.ToString(CultureInfo.InvariantCulture);
                    break;
                case STU_43CA1D94 stu: // RulesetSchemaValueBool
                    configValue.Value = stu.m_value == 1 ? ((RulesetSchemaValueBool) rulesetInfo.Value).TrueText : ((RulesetSchemaValueBool) rulesetInfo.Value).FalseText;
                    break;
                default:
                    Debugger.Break();
                    break;
            }

            return configValue;
        }
    }
}
