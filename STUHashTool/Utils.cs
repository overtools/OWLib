using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CASCLib;
using DataTool.Helper;
using OverTool;
using OWLib;
using STULib;
using STULib.Impl;
using STULib.Types;
using STULib.Types.AnimationList.x020;
using STULib.Types.Gamemodes;
using STULib.Types.GameParams;
using STULib.Types.STUUnlock;
using static STULib.Types.Generic.Common;
using static DataTool.Helper.IO;
using Console = Colorful.Console;
using Util = OverTool.Util;

namespace STUHashTool {
    public class Utils {
        internal static FieldInfo[] GetFields(Type type) {
            FieldInfo[] parent = new FieldInfo[0];
            if (type.BaseType != null && type.BaseType.Namespace != null &&
                !type.BaseType.Namespace.StartsWith("System.")) parent = GetFields(type.BaseType);
            return parent.Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                                BindingFlags.DeclaredOnly)).ToArray();
        }

        internal static string GetOWString(ulong guid, CASCHandler handler, Dictionary<ulong, Record> map) {
            if (guid == 0 || !map.ContainsKey(guid)) return null;
            Stream str = Util.OpenFile(map[guid], handler);
            OWString ows = new OWString(str);
            return ows;
        }

        internal static STUInstance[] GetInstances(ulong guid, CASCHandler handler, Dictionary<ulong, Record> map) {
            Stream str = Util.OpenFile(map[guid], handler);
            ISTU stu = ISTU.NewInstance(str, uint.MaxValue);
            return stu.Instances.ToArray();
        }

        public static void DumpSTUFull(Version2 stu, CASCHandler handler, Dictionary<ulong, Record> map,
            string instanceWildcard = null) {
            // tries to properly dump an STU to the console
            // uses handler to load GUIDs, and process the types
            
            // colour support doesn't work as well as I had hoped, because Windows is bad.
            // after setitting the colour 16 times it breaks, so I have to reset the colours every time
            // this means that any past-written colours will change.

            if (stu == null) return;
            
            STUDebugger debugger = new STUDebugger(handler, map);
            
            debugger.DumpSTU(stu, instanceWildcard);
        }
        
        public static bool IsSubclassOfRawGeneric(Type generic, Type toCheck) {
            while (toCheck != null && toCheck != typeof(object)) {
                Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur) {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

            
        public static string GetGUIDTypeName(ulong guid) {
            Dictionary<ushort, string> types = new Dictionary<ushort, string> {
                [0x90] = "encryption_key",
                [0x7C] = "string", [0xA9] = "string",
                [0x9E] = "loadout",
                [0xD5] = "description",
                [0x58] = "hero_unlocks",
                [0xA5] = "unlock",
                [0x75] = "hero",
                [0x71] = "subtitle",
                [0x68] = "achievement",
                [0xD9] = "brawl_name",
                [0x9F] = "map",
                [0xC5] = "gamemode",
                [0xC6] = "game_ruleset_schema"
            };
            return types.ContainsKey(GUID.Type(guid)) ? types[GUID.Type(guid)] : null;
        }

        public class STUDebugger {
            private readonly CASCHandler _handler;
            private readonly Dictionary<ulong, Record> _map;
            
            public STUDebugger(CASCHandler handler, Dictionary<ulong, Record> map) {
                _handler = handler;
                _map = map;
            }

            public static Dictionary<string, string> ValueRenames = new Dictionary<string, string> {
                {"UInt32", "u32"}, {"Int32", "i32"}, {"Byte", "u8"}, {"String", "string"}, {"UInt64", "u64"}, 
                {"Int64", "i64"}, {"Single", "f32"} , {"SByte", "i8"}
            };

            private static string GetType(FieldInfo fieldInfo, Type type) {
                string ret = null;
                if (!type.IsClass || type == typeof(string)) {
                    string valueName = type.Name;
                    if (ValueRenames.ContainsKey(valueName)) valueName = ValueRenames[valueName];
                    ret = valueName;
                }
                if (type == typeof(STUGUID)) ret = "guid";
                if (ret == null && type.IsArray) ret = type.GetElementType()?.Name;
                if (ret == null) ret = type.Name;

                if (IsSubclassOfRawGeneric(typeof(STUHashMap<>), type)) {
                    Type subType = type.GetGenericArguments()[0];
                    ret = $"hashmap<{subType.Name}>";
                }

                if (fieldInfo != null) {
                    STUFieldAttribute element = fieldInfo.GetCustomAttribute<STUFieldAttribute>();
                    if (element != null && typeof(STUInstance).IsAssignableFrom(type)) {
                        ret = element.EmbeddedInstance ? $"embed<{ret}>" : $"inline<{ret}>";
                    }
                }
                if (type.IsArray) ret = $"array<{ret}>";
                return ret;
            }

            private string DumpGUID(IndentHelper indentHelper, ulong guid) {
                string baseString = $"\r\n{indentHelper + 1}[{GetGUIDTypeName(guid)}]";
                STUInstance[] instances;
                switch (GUID.Type(guid)) {
                    default:
                        return "";
                    case 0x7C:
                    case 0xA9:
                        return $"{baseString} \"{GetOWString(guid, _handler, _map)}\"";
                    case 0x90:
                        instances = GetInstances(guid, _handler, _map);
                        if (instances[0] == null) return null;
                        STUEncryptionKey encryptionKey = instances[0] as STUEncryptionKey;
                        return $"{baseString} {encryptionKey?.KeyNameProper}:{encryptionKey?.Key}";
                    case 0xA5:
                        instances = GetInstances(guid, _handler, _map);
                        Cosmetic unlock = instances.OfType<Cosmetic>().First();
                        string baseString2 = $"\r\n{indentHelper + 1}[{GetGUIDTypeName(guid)}";
                        if (unlock == null) return null;
                        if (unlock is Currency) {
                            return $"{baseString2}:Credits] {(unlock as Currency).Amount} Credits";
                        } else if (unlock is Portrait) {
                            Portrait portrait = unlock as Portrait;
                            return
                                $"{baseString2}:Credits] {portrait.Tier} Star:{portrait.Star} Level:{portrait.Level}";
                        } else if (unlock is CompetitiveCurrencyReward) {
                            CompetitiveCurrencyReward competitiveCurrencyReward = unlock as CompetitiveCurrencyReward;
                            return
                                $"{baseString2}:CompetitivePoints] {competitiveCurrencyReward.Amount} points";
                        } else {
                            return $"{baseString2}:{unlock.GetType().Name}] \"{GetOWString(unlock.CosmeticName, _handler, _map)}\"";
                        }
                    case 0x9E:
                        instances = GetInstances(guid, _handler, _map);
                        if (instances[0] == null) return null;
                        STULoadout ability = instances[0] as STULoadout;
                        return $"{baseString} \"{GetOWString(ability?.Name, _handler, _map)}\" ({ability?.Category})";
                    case 0xC5:
                        instances = GetInstances(guid, _handler, _map);
                        if (instances[0] == null) return null;
                        STUGamemode gamemode = instances[0] as STUGamemode;
                        return $"{baseString} \"{GetOWString(gamemode?.DisplayName, _handler, _map)}\"";
                    case 0xC6:
                        instances = GetInstances(guid, _handler, _map);
                        if (instances[0] == null) return null;
                        STUGameParamBase gameRulsetSchema = instances[0] as STUGameParamBase;
                        return $"{baseString} \"{GetOWString(gameRulsetSchema?.Name, _handler, _map)}\"";
                    case 0x75:
                        instances = GetInstances(guid, _handler, _map);
                        if (instances[0] == null) return null;
                        STUHero hero = instances[0] as STUHero;
                        return $"{baseString} \"{GetOWString(hero?.Name, _handler, _map)}\"";
                }
            }

            private string DumpField(FieldInfo fieldInfo, object value, IndentHelper helper, List<object> recursionGuard, Type type=null) {
                if (value == null) {
                    if (type != null) return $"[{GetType(fieldInfo, type)}] null";
                    return $"[{GetType(fieldInfo, fieldInfo.FieldType)}] null";
                }
                if (!value.GetType().IsClass || value is string) {
                    return $"[{GetType(fieldInfo, value.GetType())}] {value}";
                }
                if (value.GetType() == typeof(STUGUID)) {
                    return $"[{GetType(fieldInfo, value.GetType())}] {GetFileName((STUGUID) value)}{DumpGUID(helper, (STUGUID) value)}";
                }
                if (value.GetType().IsArray) {
                    if (!(value is IEnumerable enumerable)) throw new InvalidDataException();
                    int index = 0;

                    Type elementType = null;
                    Type[] interfaces = value.GetType().GetInterfaces();
                    foreach (Type i in interfaces)
                        if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                            elementType = i.GetGenericArguments()[0];
                    
                    string ret = "";
                    foreach (object arrayValue in enumerable) {
                        ret += $"\r\n{helper+1}[{index}]: {DumpField(null, arrayValue, helper+1, recursionGuard, elementType)}";
                        index++;
                    }

                    return $"[array<{GetType(fieldInfo, value.GetType().GetElementType())}>] Count={index}" + ret;
                }
                if (value is IDictionary dict) {
                    List<object> keys = new List<object>();
                    foreach (object key in dict.Keys) {
                        keys.Add(key);
                    }
                    // todo: this means no standard dict(?)
                    Type valueType = value.GetType().GetGenericArguments()[0];
                    
                    string ret = $"[{GetType(fieldInfo, value.GetType())}] Count={keys.Count}";

                    int valueIndex = 0;
                    foreach (object dictValue in dict.Values) {
                        ret += $"\r\n{helper + 1}[{keys[valueIndex]}]: {DumpField(null, dictValue, helper + 1, recursionGuard, valueType)}";
                        valueIndex++;
                    }
                    return ret;
                }
                if (value.GetType().IsClass) {
                    if (recursionGuard.Contains(value)) return $"[{GetType(fieldInfo, value.GetType())}] (Recursion Error)";
                    List<object> newRecursionGuard = new List<object>(recursionGuard) {value};
                    
                    // make viewing graphs nicer
                    if (fieldInfo != null && fieldInfo.Name == "ParentNode" && fieldInfo.DeclaringType == typeof(STUGraphPlug)) {
                        return $"[{GetType(fieldInfo, value.GetType())}] NodeReference: m_uniqueID: [u32] {(value as STUGraphNode)?.UniqueID.ToString() ?? "null"}";
                    }
                    
                    return $"[{GetType(fieldInfo, value.GetType())}] \r\n{DumpInstance(value, helper+1, newRecursionGuard)}";
                }
                return "";
            }

            private string DumpInstance(object instance, IndentHelper indentHelper, List<object> recursionGuard) {
                if (instance == null) {
                    return $"{indentHelper}STU_: unregistered";
                }
                FieldInfo[] fields = GetFields(instance.GetType()).Where(
                    fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0).ToArray();

                string ret = "";
                string lineStart = "";
                List<object> newRecursionGuard = new List<object>(recursionGuard) {instance};
                foreach (FieldInfo field in fields) {
                    if (field == null) continue;
                    STUFieldAttribute element = field.GetCustomAttribute<STUFieldAttribute>();
                    string fieldNameThing = element.Name == null ? $"{element.Checksum:X8}" : $"{element.Name}";
                    ret += $"{lineStart}{indentHelper}{field.DeclaringType?.Name}.{field.Name}|{fieldNameThing}: {DumpField(field, field.GetValue(instance), indentHelper, newRecursionGuard)}";
                    lineStart = "\r\n";
                }
                return ret;
            }

            public void DumpSTU(ISTU stu, string instanceWildcard) {
                IndentHelper indentHelper = new IndentHelper();
                int index = 0;
                Console.Out.WriteLine($"Instances: [array] Count={stu.Instances.Count()}");
                foreach (STUInstance instance in stu.Instances) {
                    if (instance == null) {
                        index++;
                        continue;
                    }
                    if (instance.Usage != InstanceUsage.Root) {
                        index++;
                        continue;
                    }
                    string[] instanceChecksums = instance.GetType().GetCustomAttributes<STUAttribute>().Select(x => Convert.ToString(x.Checksum, 16)).ToArray();
                    if (instanceWildcard != null && !instanceChecksums.Any(x => string.Equals(x, instanceWildcard.TrimStart('0'),  StringComparison.InvariantCultureIgnoreCase))) continue;
                    
                    Console.Out.WriteLine($"{indentHelper + 1}[{index}]: [{instance.GetType().Name}]");
                    Console.Out.WriteLine(DumpInstance(instance, indentHelper + 2, new List<object>()));
                    index++;
                }
            }
        }
    }
}
