using CASCExplorer;
using OWLib;
using STULib.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using OverTool;
using STULib;
using STULib.Types;
using static STULib.Types.Generic.Common;

namespace STUHashTool {
    public class Utils {       
        internal static FieldInfo[] GetFields(Type type) {
            FieldInfo[] parent = new FieldInfo[0];
            if (type.BaseType != null && type.BaseType.Namespace != null && !type.BaseType.Namespace.StartsWith("System.")) {
                parent = GetFields(type.BaseType);
            }
            return parent.Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)).ToArray();
        }

        public static string GetIndentString(uint indentLevel) {
            //return $"{indentLevel.ToString()}: ";
            return string.Concat(Enumerable.Repeat("    ", (int) indentLevel));
        }

        internal static string GetOWString(STUGUID guid, CASCHandler handler, Dictionary<ulong, Record> map) {
            if (guid == null || !map.ContainsKey(guid)) {
                return null;
            }
            Stream str = OverTool.Util.OpenFile(map[guid], handler);
            OWString ows = new OWString(str);
            return ows;
        }

        internal static STUInstance[] GetInstances(STUGUID guid, CASCHandler handler, Dictionary<ulong, Record> map) {
            Stream str = OverTool.Util.OpenFile(map[guid], handler);
            ISTU stu = ISTU.NewInstance(str, uint.MaxValue);
            return stu.Instances.ToArray();
        }

        public static string GetGUIDProcessed(STUGUID guid, CASCHandler handler, Dictionary<ulong, Record> map, 
            uint indentLevel) {
            string indentString = GetIndentString(indentLevel);
            try {
                if (!map.ContainsKey(guid))
                    return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3} virtual";
                STUInstance[] instances;
                switch (GUID.Type(guid)) {
                    default:
                        return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3} unknown type";
                    case 0x7C:
                    case 0xA9:
                        return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3}|String: \"{GetOWString(guid, handler, map)}\"";
                    case 0x9E:
                        instances = GetInstances(guid, handler, map);
                        if (instances[0] == null) {
                            return null;
                        }
                        STUAbilityInfo ability = instances[0] as STUAbilityInfo;
                        return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3}|Ability: \"{GetOWString(ability?.Name, handler, map)}\" : {ability?.AbilityType}";
                    case 0x90:
                        instances = GetInstances(guid, handler, map);
                        if (instances[0] == null) {
                            return null;
                        }
                        // STUEncryptionKey encryptionKey = instances[0] as STUEncryptionKey;
                        return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3}|EncryptionKey: BROKEN";
                    // return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3}|EncryptionKey: {encryptionKey?.KeyNameText} : {encryptionKey?.KeyValueText}";
                    //todo: wait for fix
                    case 0xD5:
                        instances = GetInstances(guid, handler, map);
                        if (instances[0] == null) {
                            return null;
                        }
                        STUDescription description = instances[0] as STUDescription;
                        return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3}|Description: \"{GetOWString(description?.String, handler, map)}\"";
                }
            } catch (Exception) {
                return $"{indentString}{GUID.LongKey(guid):X12}.{GUID.Type(guid):X3} BLTEDecoderException";
            }
        }

        internal static void DumpSTUInner(object instance, FieldInfo[] fields, CASCHandler handler, 
            Dictionary<ulong, Record> map, uint indentLevel) {
            string indentString = GetIndentString(indentLevel);
            foreach (FieldInfo field in fields) {
                STUFieldAttribute element = field.GetCustomAttribute<STUFieldAttribute>();
                if (element == null) {
                    continue;
                }
                object fieldValue = field.GetValue(instance);
                
                string nameStringBase = element.Name != null
                    ? $"{element.Name}|{field.Name}"
                    : $"{element.Checksum:X8}|{field.Name}";

                string nameString = $"{indentString}{nameStringBase}:";
                if (fieldValue == null) {
                    Console.Out.WriteLine($"{nameString} null");
                    continue;
                }
                if (field.FieldType.IsArray) {
                    Console.Out.WriteLine($"{nameString}");
                    if (field.FieldType == typeof(STUGUID[])) {
                        foreach (STUGUID val in (STUGUID[]) fieldValue) {
                            Console.Out.WriteLine($"{GetGUIDProcessed(val, handler, map, indentLevel+1)}");
                        }
                    } else if (field.FieldType.IsClass) {
                        uint index = 0;
                        foreach (object val in (object[]) fieldValue) {
                            Console.Out.WriteLine($"{GetIndentString(indentLevel+1)}{nameStringBase}[{index}]:");
                            DumpSTUInner(val, GetFields(val.GetType()).OrderBy(x => x.Name).Where(
                                    fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0).ToArray(), 
                                handler, map, indentLevel+2);
                            index++;
                        }
                        
                    } else {
                        foreach (object val in (object[]) fieldValue) {
                            Console.Out.WriteLine($"{GetIndentString(indentLevel+1)}{val}");   
                        }
                    }
                } else {
                    if (field.FieldType == typeof(STUGUID)) {
                        string output = GetGUIDProcessed((STUGUID) fieldValue, handler, map, indentLevel+1);
                        if (output == null) {
                            Console.Out.WriteLine($"{nameString}");
                        }
                        Console.Out.WriteLine($"{nameString}\r\n{output}");
                    } else if (field.FieldType.IsClass) {
                        //Console.Out.WriteLine($"{indentString}{fieldValue.GetType()}:");
                        Console.Out.WriteLine($"{nameString}");
                        DumpSTUInner(fieldValue, GetFields(fieldValue.GetType()).OrderBy(x => x.Name).Where(
                            fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0).ToArray(), 
                            handler, map, indentLevel+1);
                    } else {
                        Console.Out.WriteLine($"{nameString} {fieldValue}");
                    }
                }
//                if (field.Name == "CosmeticUnknownArray") {
//                    if (Debugger.IsAttached) {
//                        Debugger.Break();
//                    }
//                }
            }
        }

        public static void DumpSTUFull(Version2 stu, CASCHandler handler, Dictionary<ulong, Record> map, 
            string instanceWildcard = null) {
            // tries to properly dump an STU to the console
            // uses handler to load GUIDs, and process the types

            foreach (STUInstance instance in stu.Instances) {
                if (instance == null) continue;
                string instanceChecksum = Convert.ToString(instance.GetType().GetCustomAttribute<STUAttribute>().Checksum, 16);
                if (instanceWildcard != null && !string.Equals(instanceChecksum, instanceWildcard.TrimStart('0'), 
                        StringComparison.InvariantCultureIgnoreCase)) {
                    continue;
                }
                Console.Out.WriteLine($"Found instance: {instance.GetType()}");
                DumpSTUInner(instance, GetFields(instance.GetType()).OrderBy(x => x.Name).Where(
                    fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0).ToArray(), handler, 
                    map, 1);
                if (Debugger.IsAttached) {
                    Debugger.Break();
                }
            }
        }
    }
}
