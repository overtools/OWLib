using System;
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
    [Tool("list-workshop", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListWorkshop : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(data, flags);
                    return;
                }
        }

        private static WorkshopContainer GetData() {
            var @return = new WorkshopContainer();

            foreach (ulong key in TrackedFiles[0x54]) {
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
                            Parameters = ParseParameters(action.m_params)
                        });
                        break;
                    case STU_8C73C07E stu:
                        @return.Events = stu.m_targets.Select(action => new WorkshopDefinition {
                            DisplayName = GetString(action.m_displayName),
                            Description = GetString(action.m_description),
                            GraphId = action.m_graph,
                            UnkByte = action.m_89C93A57,
                            Parameters = ParseParameters(action.m_params)
                        });

                        @return.Unknown = stu.m_16EC4AA9?.Select(x => new WorkshopDefinition {
                            DisplayName = GetString(x.m_displayName),
                            Description = GetString(x.m_description),
                            GraphId = x.m_graph,
                            UnkByte = x.m_89C93A57,
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
                               @out.InferredType = "WaitThing";
                               break;
                           case STU_C5BE2B08 ss:
                               @out.InferredType = "GenericInput";
                               @out.InputType = ss.m_16CCEFC8;
                               @out.UnkEnum = ss.m_444416F6;
                               @out.Min = ss.m_min;
                               @out.Max = ss.m_max;
                               @out.InputId = ss.m_464FB148;
                               @out.UnkByte = ss.m_89C93A57;
                               @out.DefaultValue = ss.m_D62358FA;
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
                               @out.Name = GetString(ss.m_B9AD8659);
                               break;
                           case STU_8504E8FE ss:
                               @out.InferredType = "ComparisonThing";
                               break;
                           case STU_CFF9EFAB ss:
                               @out.InferredType = "SomeFakeDebugThingy";
                               @out.Name = GetString(ss.m_B9AD8659);
                               break;
                           case STU_F654E6FB ss:
                               @out.InferredType = "CustomString";
                               break;
                           case STU_218BCF68 ss:
                               @out.InferredType = "CustomString2"; // wot ??
                               break;
                           case STU_E08C5126 ss:
                               @out.InferredType = "Player"; // Only used on events??
                               break;
                           case STU_16886813 ss:
                               @out.InferredType = "Team"; // Only used on events??
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
            public IEnumerable<string> TextOptions;
        }
        
        public class WorkshopDefinition {
            public string DisplayName;
            public string Description;
            public teResourceGUID GraphId;
            public byte UnkByte;
            public IEnumerable<WorkshopParameter> Parameters;
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

            public teResourceGUID InputId;
            public Enum_542A081B InputType;
            public Enum_43D38C2E UnkEnum;
            public float Max;
            public float Min;
            public byte UnkByte;
            public float DefaultValue;

            // utf8 doesnt really support polymorphism so.... we gotta do this
            public bool ShouldSerializeDropdownId() => InferredType == "Dropdown";
            public bool ShouldSerializeInputType() => InferredType == "GenericInput";
            public bool ShouldSerializeUnkEnum() => InferredType == "GenericInput";
            public bool ShouldSerializeInputId() => InferredType == "GenericInput";
            public bool ShouldSerializeUnkByte() => InferredType == "GenericInput";
            public bool ShouldSerializeDefaultValue() => InferredType == "GenericInput";
            public bool ShouldSerializeMax() => InferredType == "GenericInput" || InferredType == "NumberConstant";
            public bool ShouldSerializeMin() => InferredType == "GenericInput" || InferredType == "NumberConstant";
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
    }
}