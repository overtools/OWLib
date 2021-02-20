using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TankLib;
using TankLib.Math;
using TankLib.STU;

namespace TankLibHelper {
    public class InstanceBuilder : ClassBuilder {
        private readonly string _parentName;
        private readonly InstanceNew _instance;

        public override bool HasRealName => Info.KnownInstances.ContainsKey(_instance.Hash2); 
        
        public InstanceBuilder(StructuredDataInfo info, InstanceNew instance) : base(info) {
            _instance = instance;            
            
            Name = Info.GetInstanceName(_instance.Hash2);
            if (instance.ParentHash2 != 0) {
                _parentName = Info.GetInstanceName(_instance.ParentHash2);
                
                if (!info.Instances.ContainsKey(_instance.ParentHash2)) {
                    Console.Out.WriteLine($"pls fix: {_instance.Hash2:X32}'s parent is missing (add to ignored)");
                }
            }
        }
        
        private static readonly List<string> ImportTankMathTypes = new List<string> {"teColorRGB", "teColorRGBA", "teEntityID", 
             "teMtx43A", "teQuat", "teUUID", "teVec4", "teVec3A", "teVec3", "teVec2", "DBID"};

        public override IndentedTextWriter Build(FileWriter file) {
            if (_instance.ParentHash2 != 0 && !Info.Instances.ContainsKey(_instance.ParentHash2)) {
                return null; // parent isn't registered for some reason
            }

            var writer = new IndentedTextWriter(new StringWriter(), "    ");
            
            //if (Info.KnownInstances.ContainsKey(_instance.Hash2)) {
            //    writer.WriteLine($"[STU(0x{_instance.Hash2:X8}, \"{Name}\")]");
            //} else {
            writer.WriteLine($"[STU(0x{_instance.Hash2:X8}, {_instance.m_size})]");
            //}

            if (_instance.ParentHash2 == 0) {
                writer.WriteLine($"public class {Name} : {nameof(STUInstance)}");
            } else {
                writer.WriteLine($"public class {Name} : {_parentName}");
            }
            writer.WriteLine("{");
            writer.Indent++;

            bool first = true;
            foreach (FieldNew field in _instance.m_fields) {
                if (first) first = false;
                else writer.WriteLine();

                BuildField(field, writer);

                if (ImportTankMathTypes.Contains(field.m_typeName)) 
                    file.m_includes.Add("TankLib.Math");
                if (field.m_serializationType == 8 || field.m_serializationType == 9) 
                    file.m_includes.Add("TankLib.STU.Types.Enums");
            }

            writer.Indent--;
            writer.Write("}");  // close class
            
            file.m_children.Add(writer);
                
            return writer;
        }

        private void BuildField(FieldNew field, IndentedTextWriter builder) {
            string linePrefix = string.Empty;
            if (field.m_serializationType == 2 || field.m_serializationType == 3 || field.m_serializationType == 4 || 
                field.m_serializationType == 5 || field.m_serializationType == 7) {
                if (!Info.Instances.ContainsKey(field.TypeHash2)) {
                    linePrefix = "// ";
                }
            }
            
            string attribute;
            {
                attribute = $"[STUField(0x{field.Hash2:X8}, {field.m_offset}";

                //if (Info.KnownFields.ContainsKey(field.Hash2)) {
                //    attribute += $", \"{Info.GetFieldName(field.Hash2)}\"";
                //}

                if (field.m_serializationType == 2 || field.m_serializationType == 3) {  // 2 = embed, 3 = embed array
                    attribute += $", ReaderType = typeof({nameof(EmbeddedInstanceFieldReader)})";
                }

                if (field.m_serializationType == 4 || field.m_serializationType == 5) {  // 4 = inline, 5 = inline array
                    attribute += $", ReaderType = typeof({nameof(InlineInstanceFieldReader)})";
                }
            
                attribute += $")] // size: {field.m_size}"; 
            }

            var fieldName = Info.GetFieldName(field.Hash2);
            var actualType = GetFieldTypeCSharp(field);
            if (actualType == null) {
                builder.WriteLine($"// {fieldName}: unsupported data type {field.m_typeName}");
                return;
            }
            
            var postField = GetFieldPostTypeCSharp(field);
            var defaultVal = GetDefaultValue(field);
            var type = actualType + postField;
            builder.WriteLine($"{linePrefix}{attribute}");
            builder.Write($"{linePrefix}public {type} {fieldName}");
            if (defaultVal != null) {
                builder.Write($" = {defaultVal}");
            }
            builder.WriteLine(";");

            // todo: // {field.m_offset} - size: {field.m_size}
            // todo: why was i bullied into removing this
            
            // todo: what is going on with stuunlock
            // todo: the todo above is not descriptive enough for me to have any idea what it means
        }

        private string GetFieldPostTypeCSharp(FieldNew field) {
            if (field.m_serializationType == 1 || field.m_serializationType == 3 || field.m_serializationType == 5 ||
                field.m_serializationType == 9 || field.m_serializationType == 11 || field.m_serializationType == 13) {
                return "[]";
            }
            return null;
        }
        

        private string GetFieldTypeCSharp(FieldNew field) {
            if ((field.m_serializationType == 2 || field.m_serializationType == 3 || field.m_serializationType == 4 ||
                 field.m_serializationType == 5) && string.IsNullOrEmpty(field.m_typeName)) {
                uint hash = field.TypeHash2;

                var instName = Info.GetInstanceName(hash);
                if (instName == "teStructuredData") {
                    instName = "STUInstance";
                }
                return instName;
            }

            if (field.m_serializationType == 7) {
                return $"{nameof(teStructuredDataHashMap<STUInstance>)}<{Info.GetInstanceName(field.TypeHash2)}>";
            }

            if (field.m_serializationType == 8 || field.m_serializationType == 9) {  // 8 = enum, 9 = enum array
                return Info.GetEnumName(field.TypeHash2);
            }

            if (field.m_serializationType == 10 || field.m_serializationType == 11) {
                return nameof(teStructuredDataAssetRef<ulong>) + "<ulong>";
            }

            if (field.m_serializationType == 12 || field.m_serializationType == 13) {
                uint hash = field.TypeHash2;
                if (!Info.Instances.ContainsKey(hash)) {
                    return nameof(teStructuredDataAssetRef<ulong>) + "<ulong>";
                }
                return nameof(teStructuredDataAssetRef<ulong>) + $"<{Info.GetInstanceName(hash)}>";
            }
            
            switch (field.m_typeName) {
                // primitives with factories
                case "u64":
                    return "ulong";
                case "u32":
                    return "uint";
                case "u16":
                    return "ushort";
                case "u8": 
                    return "byte";
                    
                case "s64":
                    return "long";
                case "s32":
                    return "int";
                case "s16":
                    return "short";
                case "s8": 
                    return "sbyte";
                
                case "f64":
                    return "double";
                case "f32":
                    return "float";
                
                case "teString":
                    return nameof(teString);
                    
                // structs
                case "teVec2":
                    return nameof(teVec2);
                case "teVec3":
                    return nameof(teVec3);
                case "teVec3A":
                    return nameof(teVec3A);
                case "teVec4":
                    return nameof(teVec4);
                case "teQuat":
                    return nameof(teQuat);
                case "teColorRGB":
                    return nameof(teColorRGB);
                case "teColorRGBA":
                    return nameof(teColorRGBA);
                case "teMtx43A":
                    return nameof(teMtx43A);  // todo: supposed to be 4x4?
                case "teEntityID":
                    return nameof(teEntityID);
                case "teUUID":
                    return nameof(teUUID);
                case "teStructuredDataDateAndTime":
                    return nameof(teStructuredDataDateAndTime);
                
                // ISerializable_STU
                case "DBID":
                    return nameof(DBID);
                
                case "bsDataStore":
                    return null; // not supported
            }
            throw new NotImplementedException();
        }

        private string GetDefaultValue(FieldNew field) {
            if (string.IsNullOrEmpty(field.m_typeName)) return null;
            if (field.m_defaultValue == null) return null;

            var typeHash = field.TypeHash2;
            if (Info.Enums.TryGetValue(typeHash, out var enumDef)) {
                var defaultVal = EnumValueNew.TruncateValue((long)field.m_defaultValue.m_value, field);
                
                var enumValues = enumDef.m_values.Where(x => x.GetSafeValue(field) == defaultVal).ToArray();

                if (enumValues.Length != 1) { // 0 or 2+
                    return $"({Info.GetEnumName(enumDef.Hash2)})0x{defaultVal:X}";
                }
                var enumValue = enumValues[0];
                return $"{Info.GetEnumName(enumDef.Hash2)}.{Info.GetEnumValueName(enumValue.Hash2)}";
            }

            if (field.m_typeName == "s32" && field.m_defaultValue.m_hexValue == "FFFFFFFF") {
                // todo: handle more generically... this is the only case for now tho
                return "-1";
            }

            switch (field.m_typeName) {
                case "f32":
                    return $"{field.m_defaultValue.m_value}f";
                case "f64":
                    return $"{field.m_defaultValue.m_value}";
                
                case "teVec2":
                    return $"new teVec2({field.m_defaultValue.m_x}f, {field.m_defaultValue.m_y}f)";
                case "teVec3":
                    return $"new teVec3({field.m_defaultValue.m_x}f, {field.m_defaultValue.m_y}f, {field.m_defaultValue.m_z}f)";
                case "teVec3A":
                    return $"new teVec3A({field.m_defaultValue.m_x}f, {field.m_defaultValue.m_y}f, {field.m_defaultValue.m_z}f, {field.m_defaultValue.m_a}f)";
                case "teVec4":
                    return $"new teVec4({field.m_defaultValue.m_x}f, {field.m_defaultValue.m_y}f, {field.m_defaultValue.m_z}f, {field.m_defaultValue.m_w}f)";
                case "teQuat":
                    return $"new teQuat({field.m_defaultValue.m_x}f, {field.m_defaultValue.m_y}f, {field.m_defaultValue.m_z}f, {field.m_defaultValue.m_w}f)";
                
                case "teColorRGB":
                    return $"new teColorRGB({field.m_defaultValue.m_r}f, {field.m_defaultValue.m_g}f, {field.m_defaultValue.m_b}f)";
                case "teColorRGBA":
                    return $"new teColorRGBA({field.m_defaultValue.m_r}f, {field.m_defaultValue.m_g}f, {field.m_defaultValue.m_b}f, {field.m_defaultValue.m_a}f)";
                
                case "teString":
                    return $"\"{field.m_defaultValue.m_value}\"";
                case "teUUID":
                    throw new NotImplementedException();
                
                default:
                    return $"0x{ulong.Parse(field.m_defaultValue.m_hexValue, NumberStyles.HexNumber):X}";
            }
        }
    }
}