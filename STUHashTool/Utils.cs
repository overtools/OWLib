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
                [0x75] = "Hero"
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
                        return $"{nameString}\r\n{DumpSTUInner(unlocks, GetFields(unlocks.GetType()).OrderBy(x => x.Name).Where(fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0).ToArray(), handler, map, indentLevel+1)}";
                    case 0xA5:
                        instances = GetInstances(guid, handler, map);
                        Cosmetic unlock = instances.OfType<Cosmetic>().First();
                        if (unlock == null) return null;
                        if (unlock is Currency) {
                            return $"{nameStringBase}:Credits: {(unlock as Currency).Amount} Credits";
                        } else {
                            return $"{nameStringBase}:{unlock.GetType().Name}: {GetOWString(unlock.CosmeticName, handler, map)}";
                        }
                    case 0x75:
                        instances = GetInstances(guid, handler, map);
                        STUHero hero = instances.OfType<STUHero>().First();
                        return $"{nameString}{GetOWString(hero?.Name, handler, map)}";
                }
            }
            catch (Exception) {
                return "BLTEDecoderException";
            }
        }

        public static string GetGUIDProcessed(STUGUID guid, CASCHandler handler, Dictionary<ulong, Record> map,
            uint indentLevel) {
            string indentString = GetIndentString(indentLevel);
            try {
                if (!map.ContainsKey(guid))
                    return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3} virtual";
                return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3}|{ProcessGUIDInternal(guid, handler, map, indentLevel)}";
            }
            catch (Exception) {
                return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3} BLTEDecoderException";
            }
        }

        internal static string DumpSTUInner(object instance, FieldInfo[] fields, CASCHandler handler,
            Dictionary<ulong, Record> map, uint indentLevel) {
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
                            continue;
                        }
                        if (field.FieldType.IsClass && !ISTU.IsSimple(field.FieldType.GetElementType())) {
                            sb.AppendLine($"\r\n{GetIndentString(indentLevel + 1)}{nameStringBase}[{index}]:");
                            sb.Append(DumpSTUInner(val, GetFields(val.GetType()).OrderBy(x => x.Name).Where(
                                    fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0)
                                .ToArray(), handler, map, indentLevel + 2));
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
                        sb.Append(DumpSTUInner(fieldValue, GetFields(fieldValue.GetType()).OrderBy(x => x.Name).Where(
                                fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0).ToArray(),
                            handler, map, indentLevel + 1));
                    } else {
                        sb.Append($"{nl}{nameString} {fieldValue}");
                    }
                }
                // if (field.Name == "Unknown4") {
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

            if (stu == null) return;

            foreach (STUInstance instance in stu.Instances.Where(x => x?.Usage == InstanceUsage.Root)) {
                string[] instanceChecksums = instance.GetType().GetCustomAttributes<STUAttribute>().Select(x => Convert.ToString(x.Checksum, 16)).ToArray();
                if (instanceWildcard != null && !instanceChecksums.Any(x => string.Equals(x, instanceWildcard.TrimStart('0'),  StringComparison.InvariantCultureIgnoreCase))) continue;
                Console.Out.WriteLine($"Found instance: {instance.GetType()}");
                // STU_7A68A730 test = instance as STU_7A68A730;
                // if (test == null) continue;
                // if (test.m_1485B834 == null) continue;
                Console.Out.WriteLine(DumpSTUInner(instance, GetFields(instance.GetType()).OrderBy(x => x.Name).Where(
                        fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0).ToArray(),
                    handler, map, 1));
                if (Debugger.IsAttached) {
                    Debugger.Break();
                }
            }
        }
    }
}
