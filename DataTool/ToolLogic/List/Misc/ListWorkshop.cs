using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Program;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-workshop", Description = "List workshop data", CustomFlags = typeof(ListFlags))]
    public class ListWorkshop : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ListFlags) toolFlags;
            var data = GetData();
            OutputJSON(data, flags);
        }

        private static readonly Dictionary<ulong, string> _valuesNameDictById = new();

        private static WorkshopContainer GetData() {
            var @return = new WorkshopContainer();

            foreach (var key in TrackedFiles[0x54]) {
                var stu = STUHelper.GetInstance<STU_A21A7043>(key);
                if (stu == null) continue;

                foreach (var val in stu.m_values) {
                    if (!_valuesNameDictById.ContainsKey(val.m_identifier))
                        _valuesNameDictById[val.m_identifier] = GetString(val.m_displayName);
                }
            }

            foreach (var key in TrackedFiles[0x54]) {
                var baseStu = STUHelper.GetInstance<STUGenericSettings_Base>(key);

                switch (baseStu) {
                    case STU_A21A7043 stu:
                        @return.TextOptions = stu.m_7169E470.Select(strGuid => GetString(strGuid));

                        @return.Dropdowns = stu.m_C87139B0.Select(dropdownGroup => new WorkshopDropdownDefinition {
                            Id = dropdownGroup.m_7533CD4C,
                            DefaultValue = dropdownGroup.m_0A0AA524,
                            Options = dropdownGroup.m_3FE1EA9E.Select(child => new WorkshopDropdownValue {
                                VirtualId = child.m_identifier,
                                DisplayName = GetString(child.m_displayName),
                                Description = GetString(child.m_description)
                            })
                        });

                        @return.Values = stu.m_values.Select(value => new WorkshopValue {
                            Id = value.m_identifier,
                            DisplayName = GetString(value.m_displayName),
                            Description = GetString(value.m_description),
                            OutputType = value.m_904BDD85,
                            UnkEnum = value.m_10038BBD,
                            UnkByte = value.m_89C93A57,
                            Parameters = ParseParameters(value.m_CF17DD30)
                        });

                        break;
                    case STU_B85A66BB stu:
                        @return.Actions = stu.m_35CA5DCD.Select(action => new WorkshopDefinition {
                            DisplayName = GetString(action.m_displayName),
                            Description = GetString(action.m_description),
                            GraphId = action.m_graph,
                            UnkByte = action.m_89C93A57,
                            Zach = action.m_64B9FD09,
                            Parameters = ParseParameters(action.m_params)
                        });

                        break;
                    case STU_ACDD45D0 stu:
                        @return.Extensions = stu.m_86397C09.Select(ext => new WorkshopExtension {
                            GUID = ext.m_identifier,
                            DisplayName = GetString(ext.m_displayName),
                            Description = GetString(ext.m_description),
                            Cost = ext.m_925E7392,
                            UnlockedValues = ext.m_assets?.Select(v => new ExtensionVal {
                                DisplayName = GetString(v.m_displayName),
                                Description = GetString(v.m_description),
                                DropdownId = v.m_7533CD4C,
                                ValueId = v.m_A5F1C73C,
                            })
                        });

                        break;
                    case STU_8C73C07E stu:
                        @return.Events = stu.m_targets.Select(action => new WorkshopDefinition {
                            DisplayName = GetString(action.m_displayName),
                            Description = GetString(action.m_description),
                            GraphId = action.m_graph,
                            UnkByte = action.m_89C93A57,
                            Zach = action.m_64B9FD09,
                            Parameters = ParseParameters(action.m_params)
                        });

                        @return.Unknown = stu.m_16EC4AA9?.Select(x => new WorkshopDefinition {
                            DisplayName = GetString(x.m_displayName),
                            Description = GetString(x.m_description),
                            GraphId = x.m_graph,
                            UnkByte = x.m_89C93A57,
                            Zach = x.m_64B9FD09,
                            Parameters = ParseParameters(x.m_params)
                        });

                        break;
                    default:
                        continue;
                }
            }

            return @return;
        }

        private static IEnumerable<WorkshopParameter> ParseParameters(STU_9F7A0E66[] parameters) {
            return parameters?
                .Where(p => p != null)
                .Select(baseStu => {
                    var @out = new WorkshopParameter {
                        STU = baseStu.GetType().Name,
                        Name = GetString(baseStu.m_B9AD8659),
                        Description = GetString(baseStu.m_description)
                    };

                    switch (baseStu) {
                        case STU_DE6A15D2 ss:
                            @out.InferredType = "Variable";
                            break;
                        case STU_7BF2036D ss:
                            @out.InferredType = "WaitBehaviour";
                            break;
                        case STU_C5BE2B08 ss:
                            @out.InferredType = "GenericInput";
                            @out.InputType = ss.m_16CCEFC8;
                            @out.UnkEnum = ss.m_444416F6;
                            @out.Min = ss.m_min;
                            @out.Max = ss.m_max;
                            @out.UnkByte = ss.m_89C93A57;
                            @out.DefaultNumberValue = ss.m_D62358FA;

                            _valuesNameDictById.TryGetValue(ss.m_464FB148, out var name);
                            @out.DefaultValue = new WorkshopParamDefaultValue {
                                Id = ss.m_464FB148,
                                DisplayName = name
                            };

                            break;
                        case STU_8302E7AC ss:
                            @out.InferredType = "NumberConstant";
                            @out.Min = ss.m_min;
                            @out.Max = ss.m_max;
                            break;
                        case STU_F5D532BC ss:
                            @out.InferredType = "TeamConstant";
                            break;
                        case STU_28E537BD ss:
                            @out.InferredType = "MapConstant";
                            break;
                        case STU_93382EAB ss:
                            @out.InferredType = "GamemodeConstant";
                            break;
                        case STU_38B39A55 ss:
                            @out.InferredType = "HeroConstant";
                            break;
                        case STU_3FA24DEA ss:
                            @out.InferredType = "Dropdown";
                            @out.DropdownId = ss.m_7533CD4C;
                            break;
                        case STU_8504E8FE ss:
                            @out.InferredType = "ComparisonThing";
                            break;
                        case STU_CFF9EFAB ss:
                            @out.InferredType = "ArrayThing";
                            break;
                        case STU_F654E6FB ss:
                            @out.InferredType = "CustomString"; // Text formed from a selection of strings and specified Values
                            break;
                        case STU_218BCF68 ss:
                            @out.InferredType = "CustomString2"; // Custom text with optional inserted Values
                            break;
                        case STU_E08C5126 ss:
                            @out.InferredType = "Player"; // Only used on events??
                            break;
                        case STU_16886813 ss:
                            @out.InferredType = "Team"; // Only used on events??
                            break;
                        case STU_27CC2BBE ss:
                            @out.InferredType = "SubroutineEvent";
                            break;
                        case STU_BE408E5C ss:
                            @out.InferredType = "SubroutineThing";
                            break;
                        case STU_EE41F5B8 ss:
                            @out.InferredType = "ButtonConstant";
                            break;
                        case STU_4DD36FA9 ss:
                            @out.InferredType = "WorkshopSettingParam"; //Used in Sort Order and Min/Max params in "Workshop setting"
                            break;
                        case STU_1554B38A ss:
                            @out.InferredType = "DefaultValue"; //Only used in "Default", debug thing?
                            break;
                        default:
                            Debugger.Break();
                            break;
                    }

                    return @out;
                });
        }

        public class WorkshopContainer {
            public IEnumerable<WorkshopDefinition> Events;
            public IEnumerable<WorkshopDefinition> Actions;
            public IEnumerable<WorkshopDefinition> Unknown;
            public IEnumerable<WorkshopValue> Values;
            public IEnumerable<WorkshopDropdownDefinition> Dropdowns;
            public IEnumerable<WorkshopExtension> Extensions;
            public IEnumerable<string> TextOptions;
        }

        public class WorkshopDefinition {
            public string DisplayName;
            public string Description;
            public teResourceGUID GraphId;
            public byte UnkByte;
            internal byte Zach;
            public IEnumerable<WorkshopParameter> Parameters;
        }

        public class ExtensionVal {
            public string DisplayName;
            public string Description;
            public teResourceGUID DropdownId;
            public teResourceGUID ValueId;
        }

        public class WorkshopExtension {
            public teResourceGUID GUID;
            public string DisplayName;
            public string Description;
            public int Cost;
            public IEnumerable<ExtensionVal> UnlockedValues; //values added to the ui when extension is on (is null for example in "Spawn More Dummy Bots")
        }

        public class WorkshopValue {
            public teResourceGUID Id;
            public string DisplayName;
            public string Description;
            public Enum_542A081B OutputType;
            public Enum_43D38C2E UnkEnum;
            public byte UnkByte;
            public IEnumerable<WorkshopParameter> Parameters;
        }

        public class WorkshopParameter {
            public string InferredType;
            public string STU;
            public string Name;
            public string Description;

            public teResourceGUID DropdownId;
            public Enum_542A081B InputType;
            public Enum_43D38C2E UnkEnum;
            public WorkshopParamDefaultValue DefaultValue;
            public float DefaultNumberValue;
            public float Max;
            public float Min;
            public float UnkByte;

            // utf8 doesnt really support polymorphism so.... we gotta do this
            public bool ShouldSerializeDropdownId() => InferredType == "Dropdown";
            public bool ShouldSerializeInputType() => InferredType == "GenericInput";
            public bool ShouldSerializeUnkEnum() => InferredType == "GenericInput";
            public bool ShouldSerializeInputId() => InferredType == "GenericInput";
            public bool ShouldSerializeUnkByte() => InferredType == "GenericInput";
            public bool ShouldSerializeDefaultValue() => InferredType == "GenericInput";
            public bool ShouldSerializeMax() => InferredType is "GenericInput" or "NumberConstant";
            public bool ShouldSerializeMin() => InferredType is "GenericInput" or "NumberConstant";
        }

        public class WorkshopDropdownDefinition {
            public teResourceGUID Id;
            public teResourceGUID DefaultValue;
            public IEnumerable<WorkshopDropdownValue> Options;
        }

        public class WorkshopDropdownValue {
            public teResourceGUID VirtualId;
            public string DisplayName;
            public string Description;
        }

        public class WorkshopParamDefaultValue {
            public teResourceGUID Id;
            public string DisplayName;
        }
    }
}
