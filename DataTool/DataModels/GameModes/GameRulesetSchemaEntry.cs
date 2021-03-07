using System.Linq;
using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels.GameModes {
    [DataContract]
    public class GameRulesetSchemaEntry {
        [DataMember]
        public string Name;

        [DataMember]
        public string TextFormat;

        [DataMember]
        public teResourceGUID Virtual01C;

        [DataMember]
        public Enum_F2F62E3D Category;

        [DataMember]
        public RulesetSchemaValue Value;

        public GameRulesetSchemaEntry(STUGameRulesetSchemaEntry entry) {
            Name = GetString(entry.m_displayText);
            Category = entry.m_category;
            TextFormat = GetString(entry.m_7DF418A5);
            Virtual01C = entry.m_3E783677;

            switch (entry.m_value) {
                case STU_118786E9 val1:
                    Value = new RulesetSchemaValueInt {
                        Min = val1.m_min,
                        Max = val1.m_max,
                        Default = val1.m_default
                    };
                    break;
                case STU_8A8AA0A4 val2:
                    Value = new RulesetSchemaValueBool {
                        TrueText = GetString(val2.m_9EC1DF9A),
                        FalseText = GetString(val2.m_03613078),
                        DefaultValue = val2.m_default
                    };
                    break;
                case STU_776E5ADD val2:
                    Value = new RulesetSchemaValueEnum {
                        DefaultValue = val2.m_default.ToString(),
                        Choices = val2.m_3FE1EA9E.Select(x => new RulesetSchemaValueEnumChoice {
                            DisplayText = GetString(x.m_displayText),
                            Identifier = x.m_identifier
                        }).ToArray()
                    };
                    break;
                case STU_A499C365 val3:
                    Value = new RulesetSchemaValueFloat {
                        Min = val3.m_min,
                        Max = val3.m_max,
                        Default = val3.m_default,
                        Unk1 = val3.m_ED39107B
                    };
                    break;
                default:
                    break;
            }
        }

        public class RulesetSchemaValue { }

        public class RulesetSchemaValueInt : RulesetSchemaValue {
            public string _Name = "RulesetSchemaValueInt";
            public int Min;
            public int Max;
            public int Default;
        }

        public class RulesetSchemaValueFloat : RulesetSchemaValue {
            public string _Name = "RulesetSchemaValueFloat";
            public float Min;
            public float Max;
            public float Default;
            public float Unk1;
        }

        public class RulesetSchemaValueBool : RulesetSchemaValue {
            public string _Name = "RulesetSchemaValueBool";
            public string TrueText;
            public string FalseText;
            public int DefaultValue;
        }

        public class RulesetSchemaValueEnum : RulesetSchemaValue {
            public string _Name = "RulesetSchemaValueEnum";
            public string DefaultValue;
            public RulesetSchemaValueEnumChoice[] Choices;
        }

        public class RulesetSchemaValueEnumChoice {
            public string DisplayText;
            public teResourceGUID Identifier;
        }
    }
}
