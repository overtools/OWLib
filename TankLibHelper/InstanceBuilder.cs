using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TankLib;
using TankLib.Math;
using TankLib.STU;

namespace TankLibHelper {
    public class InstanceBuilder : ClassBuilder {
        private readonly string _parentName;
        private readonly STUInstanceJSON _instance;
        
        public InstanceBuilder(BuilderConfig config, StructuredDataInfo info, STUInstanceJSON instance) : base(config, info) {
            _instance = instance;            
            
            Name = Info.GetInstanceName(_instance.Hash);
            if (instance.Parent != 0) {
                _parentName = Info.GetInstanceName(_instance.Parent);
                
                if (!info.Instances.ContainsKey(_instance.Parent)) {
                    Console.Out.WriteLine($"pls fix: {_instance.Hash:X32}'s parent is missing (add to ignored)");
                }
            }
        }
        
        private static readonly List<string> ImportTankMathTypes = new List<string> {"teColorRGB", "teColorRGBA", "teEntityID", 
             "teMtx43A", "teQuat", "teUUID", "teVec4", "teVec3A", "teVec3", "teVec2", "DBID"};

        public override string BuildCSharp() {
            if (_instance.Parent != 0 && !Info.Instances.ContainsKey(_instance.Parent)) {
                return null; // parent isn't registered for some reason
            }

            StringBuilder builder = new StringBuilder();
            StringBuilder importBuilder = new StringBuilder();  // ahh rewrite

            bool importedTankMath = false;
            bool importedEnums = false;

            {
                //WriteDefaultHeader(builder, "Instance", "TankLibHelper.InstanceBuilder");

                if (Info.KnownInstances.ContainsKey(_instance.Hash)) {
                    builder.AppendLine($"    [{nameof(STUAttribute)}(0x{_instance.Hash:X8}, \"{Name}\")]");
                } else {
                    builder.AppendLine($"    [{nameof(STUAttribute)}(0x{_instance.Hash:X8})]");
                }

                if (_instance.Parent == 0) {
                    builder.AppendLine($"    public class {Name} : {nameof(STUInstance)} {{");
                } else {
                    builder.AppendLine($"    public class {Name} : {_parentName} {{");
                }
            }

            int i = 0;
            foreach (STUFieldJSON field in _instance.Fields) {
                if (i != 0) {
                    builder.AppendLine();
                }
                BuildFieldCSharp(field, builder);

                if (ImportTankMathTypes.Contains(field.Type) && !importedTankMath) {
                    importedTankMath = true;
                    importBuilder.AppendLine("using TankLib.Math;");
                }

                if ((field.SerializationType == 8 || field.SerializationType == 9) && !importedEnums) {
                    importedEnums = true;
                    importBuilder.AppendLine("using TankLib.STU.Types.Enums;");
                }
                
                i++;
            }

            {
                builder.AppendLine("    }");  // close class
                builder.AppendLine("}"); // close namespace
            }

            return GetDefaultHeader("Instance", "TankLibHelper.InstanceBuilder", importBuilder.ToString()) + builder;
        }

        private void BuildFieldCSharp(STUFieldJSON field, StringBuilder builder) {
            string linePrefix = string.Empty;
            if (field.SerializationType == 2 || field.SerializationType == 3 || field.SerializationType == 4 || 
                field.SerializationType == 5 || field.SerializationType == 7) {
                if (!Info.Instances.ContainsKey(field.GetSTUTypeHash())) {
                    linePrefix = "// ";
                }
            }
            
            string attribute;
            {
                attribute = $"[{nameof(STUFieldAttribute)}(0x{field.Hash:X8}";

                if (Info.KnownFields.ContainsKey(field.Hash)) {
                    attribute += $", \"{Info.GetFieldName(field.Hash)}\"";
                }

                if (field.SerializationType == 2 || field.SerializationType == 3) {  // 2 = embed, 3 = embed array
                    attribute += $", ReaderType = typeof({nameof(EmbeddedInstanceFieldReader)})";
                }

                if (field.SerializationType == 4 || field.SerializationType == 5) {  // 4 = inline, 5 = inline array
                    attribute += $", ReaderType = typeof({nameof(InlineInstanceFieldReader)})";
                }
            
                attribute += ")]"; 
            }

            string definition;

            {
                string type = GetFieldTypeCSharp(field) + GetFieldPostTypeCSharp(field);
                definition = $"{type} {Info.GetFieldName(field.Hash)}";
            }

            builder.AppendLine($"        {linePrefix}{attribute}");
            builder.AppendLine($"        {linePrefix}public {definition};");
            
            // todo: what is going on with stuunlock
        }

        private string GetFieldPostTypeCSharp(STUFieldJSON field) {
            if (field.SerializationType == 1 || field.SerializationType == 3 || field.SerializationType == 5 ||
                field.SerializationType == 9 || field.SerializationType == 11 || field.SerializationType == 13) {
                return "[]";
            }
            return null;
        }
        

        private string GetFieldTypeCSharp(STUFieldJSON field) {
            if ((field.SerializationType == 2 || field.SerializationType == 3 || field.SerializationType == 4 ||
                 field.SerializationType == 5) && field.Type.StartsWith("STU_")) {
                uint hash = field.GetSTUTypeHash();

                return Info.GetInstanceName(hash);
            }

            if (field.SerializationType == 7) {
                uint hash = field.GetSTUTypeHash();
                return $"{nameof(teStructuredDataHashMap<STUInstance>)}<{Info.GetInstanceName(hash)}>";
            }

            if (field.SerializationType == 8 || field.SerializationType == 9) {  // 8 = enum, 9 = enum array
                return Info.GetEnumName(uint.Parse(field.Type, NumberStyles.HexNumber));
            }

            if (field.SerializationType == 10 || field.SerializationType == 11) {
                return nameof(teStructuredDataAssetRef<ulong>) + "<ulong>";
            }

            if (field.SerializationType == 12 || field.SerializationType == 13) {
                uint hash = uint.Parse(field.Type.Split('_')[1], NumberStyles.HexNumber);
                if (!Info.Instances.ContainsKey(hash)) {
                    return nameof(teStructuredDataAssetRef<ulong>) + "<ulong>";
                }
                return nameof(teStructuredDataAssetRef<ulong>) + $"<{Info.GetInstanceName(hash)}>";
            }
            
            switch (field.Type) {
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
            }
            throw new NotImplementedException();
        }
    }
}