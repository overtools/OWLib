using System;
using System.Collections.Generic;
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

        private static Container GetData() {
            foreach (ulong key in TrackedFiles[0x54]) {
                var stu = STUHelper.GetInstance<STU_A21A7043>(key);
                if (stu == null) continue;

                var @return = new Container();

                @return.TextOptions = stu.m_7169E470.Select(x => GetString(x));

                @return.Dropdowns = stu.m_C87139B0.Select(x => {
                    var @out = new DropdownGroup();
                    
                    @out.Id= x.m_7533CD4C;
                    @out.DefaultValue = x.m_0A0AA524;
                    @out.Options = x.m_3FE1EA9E.Select(child => new DropdownValue {
                        VirtualId = child.m_identifier,
                        DisplayName = GetString(child.m_displayName),
                        Description = GetString(child.m_description)
                    });
                    
                    return @out;
                });

                @return.Values = stu.m_values.Select(x => {
                    return new Value {
                        Id = x.m_identifier,
                        DisplayName = GetString(x.m_displayName),
                        Description = GetString(x.m_description),
                        Parameters = x.m_CF17DD30?.Select(child => {
                            var @out = new ValueParam {
                                STU = child.GetType().Name,
                                Description = GetString(child.m_description)
                            };

                            switch (child) {
                                case STU_DE6A15D2 ss:
                                    // some global thing??
                                    break;
                                case STU_C5BE2B08 ss:
                                    @out.InferredType = "GenericInput";
                                    @out.Name = GetString(ss.m_B9AD8659);
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
                                    @out.Name = GetString(ss.m_B9AD8659);
                                    break;
                                case STU_218BCF68 ss:
                                    @out.InferredType = "CustomString2"; // wot ??
                                    @out.Name = GetString(ss.m_B9AD8659);
                                    break;
                                default:
                                    break;
                            }

                            return @out;
                        })
                    };
                });

                return @return;
            }
            
            throw new NotImplementedException();
        }

        public class Container {
            public IEnumerable<Value> Values;
            public IEnumerable<DropdownGroup> Dropdowns;
            public IEnumerable<string> TextOptions;
        }

        public class Value {
            public teResourceGUID Id;
            public string DisplayName;
            public string Description;
            public IEnumerable<ValueParam> Parameters;
        }

        public class ValueParam {
            public string InferredType;
            public string STU;
            public string Name;
            public string Description;

            public teResourceGUID InputId;
            public Enum_542A081B InputType;
            public Enum_43D38C2E UnkEnum;
            public float Max;
            public float Min;
            public byte UnkByte;
            public float DefaultValue;

            public teResourceGUID DropdownId;

            public bool ShouldSerializeDropdownId() => InferredType == "Dropdown";
            public bool ShouldSerializeInputType() => InferredType == "GenericInput";
            public bool ShouldSerializeUnkEnum() => InferredType == "GenericInput";
            public bool ShouldSerializeInputId() => InferredType == "GenericInput";
            public bool ShouldSerializeUnkByte() => InferredType == "GenericInput";
            public bool ShouldSerializeDefaultValue() => InferredType == "GenericInput";
            public bool ShouldSerializeMax() => InferredType == "GenericInput" || InferredType == "NumberConstant";
            public bool ShouldSerializeMin() => InferredType == "GenericInput" || InferredType == "NumberConstant";
        }

        public class DropdownGroup {
            public teResourceGUID Id;
            public teResourceGUID DefaultValue;
            public IEnumerable<DropdownValue> Options;

        }

        public class DropdownValue {
            public teResourceGUID VirtualId;
            public string DisplayName;
            public string Description;
        }
    }
}