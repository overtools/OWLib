using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {   
    [DataContract]
    public class GameRulesetSchemaEntry {
        [DataMember]
        public string Name;
        
        [DataMember]
        public Enum_F2F62E3D Category;
        
        [DataMember]
        public GenericRulesetValue Value;
        
        public GameRulesetSchemaEntry(STUGameRulesetSchemaEntry entry) {
            Name = GetString(entry.m_displayText);
            Category = entry.m_category;
            Value = GetRulesetValue(entry.m_value);
        }

        public class GenericRulesetValue {}

        private class RulesetValue_Range : GenericRulesetValue {
            public string Name = "RulesetValue_Range";
            public int Min;
            public int Max;
            public int Default;
        }
        
        private class RulesetValue_RangePercentage : GenericRulesetValue {
            public string Name = "RulesetValue_RangePercentage";
            public float Min;
            public float Max;
            public float Default;
            public float Unk1;
        }
        
        private class RulesetValue_Switch : GenericRulesetValue {
            public string Name = "RulesetValue_Switch";
            public string On;
            public string Off;
            public int Default;
        }
        
        private class RulesetValue_Select : GenericRulesetValue {
            public string Name = "RulesetValue_Select";
            public string Default;
            public IEnumerable<RulesetValue_SelectOption> Options;
        }
        
        private class RulesetValue_SelectOption {
            public string Name;
            public string GUID;
        }
        
        private static GenericRulesetValue GetRulesetValue(STU_848957AF value) {
            switch (value) {
                case STU_118786E9 val1:
                    return new RulesetValue_Range {
                        Min = val1.m_min,
                        Max = val1.m_max,
                        Default = val1.m_default
                    };
                case STU_8A8AA0A4 val2:
                    return new RulesetValue_Switch {
                        On = GetString(val2.m_9EC1DF9A),
                        Off = GetString(val2.m_03613078),
                        Default = val2.m_default
                    };
                case STU_776E5ADD val2:
                    return new RulesetValue_Select {
                        Default = val2.m_default.ToString(),
                        Options = val2.m_3FE1EA9E.Select(x => new RulesetValue_SelectOption {
                            Name = GetString(x.m_displayText),
                            GUID = x.m_identifier.ToString()
                        })
                    };
                case STU_A499C365 val3:
                    return new RulesetValue_RangePercentage {
                        Min = val3.m_min,
                        Max = val3.m_max,
                        Default = val3.m_default,
                        Unk1 = val3.m_ED39107B
                    };
                default:
                    return null;
            }
        }
    }
}