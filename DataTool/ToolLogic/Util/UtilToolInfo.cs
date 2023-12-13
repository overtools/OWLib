using System;
using System.Collections.Generic;
using System.Reflection;
using DataTool.Flag;
using DataTool.JSON;
using DataTool.ToolLogic.List;
using TACTLib.Container;
using TankLib;

namespace DataTool.ToolLogic.Util {
    [Tool("util-tool-info", Description = "Export tool info", CustomFlags = typeof(ListFlags), IsSensitive = true, UtilNoArchiveNeeded = true)]
    public class UtilToolInfo : JSONTool, ITool {
        public class ToolInfo {
            public Dictionary<string, string> Version = new Dictionary<string, string>();

            public FlagGroup ToolFlags;

            public Dictionary<string, ToolGroup> ToolGroups = new Dictionary<string, ToolGroup>();

            public void AddAssemblyVersion(Assembly assembly) {
                Version[assembly.GetName().Name] = assembly.GetName().Version.ToString();
            }

            public ToolGroup GetToolGroup(Type flagType) {
                foreach (KeyValuePair<string, ToolGroup> groupPair in ToolGroups) {
                    if (groupPair.Key == flagType.Name) {
                        return groupPair.Value;
                    }
                }

                var group = new ToolGroup();
                group.Flags = new FlagGroup(flagType);
                ToolGroups[flagType.Name] = group;
                return group;
            }
        }

        public class ToolGroup {
            public FlagGroup Flags;

            public List<Tool> Tools = new List<Tool>();

            public void AddTool(Type toolType, ToolAttribute toolAttribute) {
                var tool = new Tool {
                    Name = toolAttribute.Name,
                    Keyword = toolAttribute.Keyword,
                    Description = toolAttribute.Description
                };

                if (typeof(JSONTool).IsAssignableFrom(toolType)) {
                    tool.SupportsJson = true;
                }

                if (typeof(IQueryParser).IsAssignableFrom(toolType)) {
                    tool.SupportsQuery = true;

                    var toolInstance = (IQueryParser) Activator.CreateInstance(toolType)!;

                    tool.QueryInfo = new QueryInfo {
                        DynamicChoicesKey = toolInstance.DynamicChoicesKey,
                        Types = new List<QueryTypeJSON>()
                    };
                    foreach (QueryType queryType in toolInstance.QueryTypes) {
                        var typeJson = new QueryTypeJSON {
                            Name = queryType.Name,
                            HumanName = queryType.HumanName,
                            DynamicChoicesKey = queryType.DynamicChoicesKey
                        };
                        
                        foreach (QueryTag tag in queryType.Tags) {
                            typeJson.Tags.Add(new QueryTagJSON {
                                Name = tag.Name,
                                HumanName = tag.HumanName,
                                FixedValues = tag.Options,
                                DynamicChoicesKey = tag.DynamicChoicesKey
                            });
                        }

                        tool.QueryInfo.Types.Add(typeJson);
                    }
                }

                Tools.Add(tool);
            }
        }

        public class QueryInfo {
            public string DynamicChoicesKey;

            public List<QueryTypeJSON> Types = new List<QueryTypeJSON>();
        }

        public class QueryTypeJSON {
            public string Name;
            public string HumanName;
            public string DynamicChoicesKey;
            public List<QueryTagJSON> Tags = new List<QueryTagJSON>();
        }

        public class QueryTagJSON {
            public string Name;
            public string HumanName;
            public string DynamicChoicesKey;
            public List<string> FixedValues;
        }

        public class Tool {
            public string Name;
            public string Keyword;
            public string Description;

            public bool SupportsJson;

            public bool SupportsQuery;
            public QueryInfo QueryInfo;
        }

        public class FlagGroup {
            public List<Flag> Flags;
            public string Name;
            public string Parent;

            public FlagGroup(Type type) {
                Name = type.Name;

                Flags = new List<Flag>();
                AddFlags(Flags, type);

                var parent = type.BaseType;
                if (parent != null && !parent.IsAbstract) {
                    Parent = parent.Name;
                }
            }

            private static void AddFlags(ICollection<Flag> flags, IReflect T) {
                var fields = T.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                foreach (var field in fields) {
                    var flagattr = field.GetCustomAttribute<CLIFlagAttribute>(true);
                    if (flagattr == null || flagattr.AllPositionals || flagattr.Hidden) continue;

                    var flagJson = new Flag {
                        Name = flagattr.Flag,
                        HelpText = flagattr.Help,
                        Choices = flagattr.Valid,
                        IsPositional = flagattr.Positional != -1,
                        Position = flagattr.Positional,
                        Required = flagattr.Required,
                        TakesValue = flagattr.NeedsValue,
                        ValueType = FlagValueType.String
                    };

                    if (flagattr.Parser != null) {
                        var t = flagattr.Parser[1];

                        switch (t) {
                            case "CLIFlagBoolean":
                                flagJson.ValueType = FlagValueType.Boolean;
                                break;
                            case "CLIFlagInt":
                                flagJson.ValueType = FlagValueType.Int;
                                break;
                            case "CLIFlagByte":
                                flagJson.ValueType = FlagValueType.Byte;
                                break;
                            case "CLIFlagChar":
                                flagJson.ValueType = FlagValueType.Char;
                                break;
                            default:
                                throw new Exception($"UtilCommands: unable to convert parser \"{t}\" to enum");
                        }
                    }

                    flags.Add(flagJson);
                }
            }
        }

        public enum FlagValueType {
            Invalid,
            String,
            Char,
            Boolean,
            Int,
            Byte
        }

        public class Flag {
            public string Name;
            public string HelpText;
            public bool Required;
            public bool TakesValue; // true -> --{flag}={value}. false -> --{flag}
            public FlagValueType ValueType;

            public bool IsPositional;
            public int Position;

            public string[] Choices;
        }

        public void Parse(ICLIFlags toolFlags) {
            var toolInfo = new ToolInfo();
            toolInfo.AddAssemblyVersion(typeof(UtilToolInfo).Assembly); // datatool
            toolInfo.AddAssemblyVersion(typeof(teResourceGUID).Assembly); // tanklib
            toolInfo.AddAssemblyVersion(typeof(ContainerHandler).Assembly); // tactlib

            toolInfo.ToolFlags = new FlagGroup(typeof(ToolFlags));

            var tools = Program.GetTools();

            foreach (Type toolType in tools) {
                var attribute = toolType.GetCustomAttribute<ToolAttribute>();
                if (attribute == null) continue;
                if (attribute.IsSensitive || attribute.CustomFlags == null) continue;

                var toolGroup = toolInfo.GetToolGroup(attribute.CustomFlags);
                toolGroup.AddTool(toolType, attribute);
            }

            OutputJSON(toolInfo, (ListFlags) toolFlags);
        }
    }
}
