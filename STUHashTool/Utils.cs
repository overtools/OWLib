using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CASCLib;
using OverTool;
using OWLib;
using STULib;
using STULib.Impl;
using STULib.Types;
using STULib.Types.STUUnlock;
using static STULib.Types.Generic.Common;
using Util = OverTool.Util;
using Console = Colorful.Console;
using System.Drawing;
using System.Text.RegularExpressions;
using Colorful;

namespace STUHashTool {
    public class Utils {
        internal static FieldInfo[] GetFields(Type type) {
            FieldInfo[] parent = new FieldInfo[0];
            if (type.BaseType != null && type.BaseType.Namespace != null &&
                !type.BaseType.Namespace.StartsWith("System.")) parent = GetFields(type.BaseType);
            return parent.Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                                BindingFlags.DeclaredOnly)).ToArray();
        }

        public static string GetIndentString(uint indentLevel) {
            return string.Concat(Enumerable.Repeat("    ", (int) indentLevel));
        }

        internal static string GetOWString(STUGUID guid, CASCHandler handler, Dictionary<ulong, Record> map) {
            if (guid == null || !map.ContainsKey(guid)) return null;
            Stream str = Util.OpenFile(map[guid], handler);
            OWString ows = new OWString(str);
            return ows;
        }

        internal static STUInstance[] GetInstances(STUGUID guid, CASCHandler handler, Dictionary<ulong, Record> map) {
            Stream str = Util.OpenFile(map[guid], handler);
            ISTU stu = ISTU.NewInstance(str, uint.MaxValue);
            return stu.Instances.ToArray();
        }

        internal static string GetTypeName(STUGUID guid) {
            Dictionary<ushort, string> types = new Dictionary<ushort, string> {
                [0x90] = "EncryptionKey",
                [0x7C] = "String", [0xA9] = "String",
                [0x9E] = "Ability",
                [0xD5] = "Description",
                [0x58] = "HeroUnlocks",
                [0xA5] = "Unlock",
                [0x75] = "Hero",
                [0x71] = "Subtitle",
                [0x68] = "Achievement",
                [0xD9] = "BrawlName",
                [0x9F] = "Map"
            };
            return types.ContainsKey(GUID.Type(guid)) ? types[GUID.Type(guid)] : null;
        }

        internal static string ProcessGUIDInternal(STUGUID guid, CASCHandler handler, Dictionary<ulong, Record> map, 
            uint indentLevel, bool useName=true) {
            string nameString = useName ? $"{GetTypeName(guid)}: " : "";
            string nameStringBase = useName ? $"{GetTypeName(guid)}" : "";
            // string indentString = GetIndentString(indentLevel);
            try {
                if (!map.ContainsKey(guid))
                    return "virtual";
                STUInstance[] instances;
                switch (GUID.Type(guid)) {
                    default:
                        return "unknown type";
                    case 0x7C:
                    case 0xA9:
                        return $"{nameString}\"{GetOWString(guid, handler, map)}\"";
                    case 0x9E:
                        instances = GetInstances(guid, handler, map);
                        if (instances[0] == null) return null;
                        STUAbilityInfo ability = instances[0] as STUAbilityInfo;
                        return $"{nameString}\"{GetOWString(ability?.Name, handler, map)}\" : {ability?.AbilityType}";
                    case 0x90:
                        instances = GetInstances(guid, handler, map);
                        if (instances[0] == null) return null;
                        STUEncryptionKey encryptionKey = instances[0] as STUEncryptionKey;
                        return $"{nameString}{encryptionKey?.KeyNameProper} : {encryptionKey?.Key}";
                    case 0xD5:
                        instances = GetInstances(guid, handler, map);
                        if (instances[0] == null) return null;
                        STUDescription description = instances[0] as STUDescription;
                        return $"{nameString}\"{GetOWString(description?.String, handler, map)}\"";
                    case 0x58:
                        instances = GetInstances(guid, handler, map);
                        STUHeroUnlocks unlocks = instances.OfType<STUHeroUnlocks>().First();
                        if (unlocks == null) return null;
                        // Console.Out.WriteLine($"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3}|{nameString}");
                        return $"{nameString}\r\n{DumpSTUInner(unlocks, GetFields(unlocks.GetType()).OrderBy(x => x.Name).Where(fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0).ToArray(), handler, map, indentLevel+1, null, new List<object>())}";
                    case 0xA5:
                        instances = GetInstances(guid, handler, map);
                        Cosmetic unlock = instances.OfType<Cosmetic>().First();
                        if (unlock == null) return null;
                        if (unlock is Currency) {
                            return $"{nameStringBase}:Credits: {(unlock as Currency).Amount} Credits";
                        } else if (unlock is Portrait) {
                            Portrait portrait = unlock as Portrait;
                            return $"{nameStringBase}:Portrait: {portrait.Tier} Star:{portrait.Star} Level:{portrait.Level}";
                        } else {
                            return $"{nameStringBase}:{unlock.GetType().Name}: {GetOWString(unlock.CosmeticName, handler, map)}";
                        }
                    case 0x75:
                        instances = GetInstances(guid, handler, map);
                        STUHero hero = instances.OfType<STUHero>().First();
                        return $"{nameString}{GetOWString(hero?.Name, handler, map)}";
                    case 0x71:
                        instances = GetInstances(guid, handler, map);
                        STUSubtitle subtitle = instances.OfType<STUSubtitle>().First();
                        return $"{nameString}{subtitle.Text}";
                    case 0x68:
                        instances = GetInstances(guid, handler, map);
                        STUAchievement achievement = instances.OfType<STUAchievement>().First();
                        return $"{nameString}{GetOWString(achievement?.Name, handler, map)}";
                    case 0xD9:
                        instances = GetInstances(guid, handler, map);
                        STUBrawlName brawlName = instances.OfType<STUBrawlName>().First();
                        return $"{nameString}{GetOWString(brawlName?.Name, handler, map)}";
                    case 0x9F:
                        instances = GetInstances(guid, handler, map);
                        STUMap stuMap = instances.OfType<STUMap>().First();
                        return $"{nameString}{GetOWString(stuMap?.Name, handler, map)}";
                }
            }
            catch (Exception) {
                return "BLTEDecoderException";
            }
        }

        public static string GetGUIDProcessed(STUGUID guid, CASCHandler handler, Dictionary<ulong, Record> map,
            uint indentLevel) {
            // Debugger.Break();
            // if (GUID.Type(guid).ToString("X3") == "003") Debugger.Break();
            string indentString = GetIndentString(indentLevel);
            try {
                if (!map.ContainsKey(guid))
                    return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3} virtual";
                if (GUID.Type(guid).ToString("X3") != "025" && GUID.Type(guid).ToString("X3") != "0AF" && GUID.Type(guid).ToString("X3") != "024" && GUID.Type(guid).ToString("X3") != "015" && GUID.Type(guid).ToString("X3") != "0B0" && GUID.Type(guid).ToString("X3") != "014" && GUID.Type(guid).ToString("X3") != "05A" 
                    && GUID.Type(guid).ToString("X3") != "013" && GUID.Type(guid).ToString("X3") != "003" && GUID.Type(guid).ToString("X3") != "062" && GUID.Type(guid).ToString("X3") != "00D" && GUID.Type(guid).ToString("X3") != "09E" && GUID.Type(guid).ToString("X3") != "0AC"
                    && GUID.Type(guid).ToString("X3") != "01B" && GUID.Type(guid).ToString("X3") != "021" && GUID.Type(guid).ToString("X3") != "036") Debugger.Break();
                return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3}|{ProcessGUIDInternal(guid, handler, map, indentLevel)}";
            }
            catch (Exception) {
                return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3} BLTEDecoderException";
            }
        }

        internal static string DumpSTUInner(object instance, FieldInfo[] fields, CASCHandler handler,
            Dictionary<ulong, Record> map, uint indentLevel, StyleSheet sheet, List<object> doneObjects) {
            StringBuilder sb = new StringBuilder();
            string indentString = GetIndentString(indentLevel);
            uint fieldIndex = 0;
            string nl = "";
            foreach (FieldInfo field in fields) {
                if (fieldIndex == 0) nl = "";
                if (fieldIndex > 0) nl = Environment.NewLine;
                STUFieldAttribute element = field.GetCustomAttribute<STUFieldAttribute>();
                if (element == null) continue;
                object fieldValue = field.GetValue(instance);

                string nameStringBase = element.Name != null
                    ? $"{element.Name}|{field.Name}"
                    : $"{element.Checksum:X8}|{field.Name}";

                string nameString = $"{indentString}{nameStringBase}:";
                if (fieldValue == null) {
                    sb.Append($"{nl}{nameString} null");
                    fieldIndex++;
                    continue;
                }
                if (typeof(IDictionary).IsAssignableFrom(field.FieldType)) {
                    sb.Append($"{nl}{nameString}");
                    IDictionary test = (IDictionary) fieldValue;
                    int valueIndex = 0;
                    
                    List<object> keys = new List<object>();
                    foreach (object key in test.Keys) {
                        keys.Add(key);
                    }
                    
                    foreach (object value in test.Values) {
                        sb.AppendLine($"\r\n{GetIndentString(indentLevel + 1)}{nameStringBase}[{keys[valueIndex]}]:{(value != null ? $" ({value.GetType()})" : "")}");
                        if (value == null) {
                            sb.Append($"{GetIndentString(indentLevel + 2)}null");
                        } else if (value.GetType().IsClass && !ISTU.IsSimple(value.GetType()) && !value.GetType().IsEnum) {
                            if (!doneObjects.Contains(value)) {
                                doneObjects.Add(value);
                                sb.Append(DumpSTUInner(value, GetFields(value.GetType()).OrderBy(x => x.Name).Where(
                                        fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0)
                                    .ToArray(), handler, map, indentLevel + 2, sheet, new List<object>(doneObjects)));
                            } else {
                                sb.Append($"{GetIndentString(indentLevel + 2)}RECURSION ERROR");
                            }
                        } else {
                            sb.Append($"{GetIndentString(indentLevel + 2)}{value}");
                        }
                        valueIndex++;
                    }
                    fieldIndex++;
                    continue;
                }
                if (field.FieldType.IsArray) {
                    uint index = 0;
                    if (field.FieldType.GetElementType() == typeof(char)) {
                        sb.Append($"{nl}{nameString} \"{new string((char[])fieldValue)}\"");
                        fieldIndex++;
                        continue;
                    }
                    sb.Append($"{nl}{nameString}");
                    if (field.FieldType == typeof(STUGUID[])) {
                        foreach (STUGUID val in (STUGUID[]) fieldValue)
                            sb.Append($"\r\n{GetGUIDProcessed(val, handler, map, indentLevel + 1)}");
                        fieldIndex++;
                        continue;
                    }
                    
                    IEnumerable enumerable = fieldValue as IEnumerable;
                    if (enumerable == null) {fieldIndex++; continue;}
                    foreach (object val in enumerable) {
                        if (val == null) {
                            sb.Append($"\r\n{GetIndentString(indentLevel + 1)}{nameStringBase}[{index}]: null");
                            index++;
                            continue;
                        }
                        if (field.FieldType.IsClass && !ISTU.IsSimple(field.FieldType.GetElementType()) && !field.FieldType.GetElementType().IsEnum) {
                            sb.AppendLine($"\r\n{GetIndentString(indentLevel + 1)}{nameStringBase}[{index}]:");
                            if (!doneObjects.Contains(val)) {
                                doneObjects.Add(val);
                                sb.Append(DumpSTUInner(val, GetFields(val.GetType()).OrderBy(x => x.Name).Where(
                                        fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0)
                                    .ToArray(), handler, map, indentLevel + 2, sheet, new List<object>(doneObjects)));
                            } else {
                                sb.Append($"\r\n{GetIndentString(indentLevel + 2)}RECURSION ERROR");
                            }
                            
                        } else {
                            sb.Append($"\r\n{GetIndentString(indentLevel + 1)}{nameStringBase}[{index}]: {val}");
                        }
                        index++;
                    }
                } else {
                    if (field.FieldType == typeof(STUGUID)) {
                        string output = GetGUIDProcessed((STUGUID) fieldValue, handler, map, indentLevel + 1);
                        // if (output == null) sb.AppendLine($"{nameString}");
                        sb.Append($"{nl}{nameString}\r\n{output}");
                    } else if (field.FieldType.IsClass && field.FieldType != typeof(string)) {
                        //Console.Out.WriteLine($"{indentString}{fieldValue.GetType()}:");
                        sb.AppendLine($"{nl}{nameString}");
                        if (!doneObjects.Contains(fieldValue)) {                            
                            doneObjects.Add(fieldValue);
                            sb.Append(DumpSTUInner(fieldValue, GetFields(fieldValue.GetType()).OrderBy(x => x.Name)
                                    .Where(
                                        fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0)
                                    .ToArray(),
                                handler, map, indentLevel + 1, sheet, new List<object>(doneObjects)));
                        } else {
                            sb.Append($"{GetIndentString(indentLevel + 1)}RECURSION ERROR");
                        }
                        
                    } else {
                        sb.Append($"{nl}{nameString} {fieldValue}");
                    }
                    if (field.FieldType.GetInterfaces().Contains(typeof(ISTUHashToolPrintExtender))) {
                        Color? color;
                        string output = ((ISTUHashToolPrintExtender) fieldValue).Print(out color);
                        string outputFormatted = $"\r\n{GetIndentString(indentLevel + 1)}{output}";
                        string outputEscaped = Regex.Escape(outputFormatted);
                        sb.Append(outputFormatted);
                        if (color != null) {
                            if (sheet.Styles.All(x => x.Target.Value != outputEscaped)) {
                                sheet.AddStyle(outputEscaped, (Color) color);
                            }
                        }
                        
                    }

                }
                // if (field.Name == "m_961B4853") {
                //     if (Debugger.IsAttached) {
                //         Debugger.Log(0, "STUHashTool", $"\r\n{sb}\r\n");
                //         Debugger.Break();
                //     }
                // }
                fieldIndex++;
            }
            return sb.ToString();
        }

        public static void DumpSTUFull(Version2 stu, CASCHandler handler, Dictionary<ulong, Record> map,
            string instanceWildcard = null) {
            // tries to properly dump an STU to the console
            // uses handler to load GUIDs, and process the types
            
            // colour support doesn't work as well as I had hoped, because Windows is bad.
            // after setitting the colour 16 times it breaks, so I have to reset the colours every time
            // this means that any past-written colours will change.

            if (stu == null) return;

            uint index = 0;
            foreach (STUInstance instance in stu.Instances) {
                // ifs are seperate for modularity, comment and uncomment for testing
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
                Console.ReplaceAllColorsWithDefaults();
                Console.WriteLine($"Found instance: {instance.GetType()} ({index})", Color.LightGray);
                // STUAchievement test = instance as STUAchievement;
                // if (test == null) continue;
                // Debug.Assert(test.PCRewardPoints == test.XboxGamerscore);
                StyleSheet styleSheet = new StyleSheet(Color.LightGray);
                Console.WriteLineStyled(DumpSTUInner(instance, GetFields(instance.GetType()).OrderBy(x => x.Name).Where(
                        fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0).ToArray(),
                    handler, map, 1, styleSheet, new List<object>()), styleSheet);
                
                // if (Debugger.IsAttached) {
                //     Debugger.Break();
                // }
                index++;
            }
        }
    }
}
