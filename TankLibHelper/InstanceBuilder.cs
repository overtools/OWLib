using System;
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
            }
        }

        public override string BuildCSharp() {
            StringBuilder builder = new StringBuilder();

            {
                WriteDefaultHeader(builder, "Instance", "TankLibHelper.InstanceBuilder");

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
                i++;
            }

            {
                builder.AppendLine("    }");  // close class
                builder.AppendLine("}"); // close namespace
            }

            return builder.ToString();
        }

        private void BuildFieldCSharp(STUFieldJSON field, StringBuilder builder) {
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

            builder.AppendLine($"        {attribute}");
            builder.AppendLine($"        public {definition};");
            
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
                uint hash = uint.Parse(field.Type.Split('_')[1], NumberStyles.HexNumber);

                return Info.GetInstanceName(hash);
            }

            if (field.SerializationType == 7) {
                uint hash = uint.Parse(field.Type.Split('_')[1], NumberStyles.HexNumber);
                return $"teStructuredDataHashMap<{Info.GetInstanceName(hash)}>";
            }

            if (field.SerializationType == 8 || field.SerializationType == 9) {  // 8 = enum, 9 = enum array
                return Info.GetEnumName(uint.Parse(field.Type, NumberStyles.HexNumber));
            }

            if (field.SerializationType == 10 || field.SerializationType == 11) {
                return nameof(teStructuredDataAssetRef<ulong>) + "<ulong>";
            }

            if (field.SerializationType == 12 || field.SerializationType == 13) {
                uint hash = uint.Parse(field.Type.Split('_')[1], NumberStyles.HexNumber);
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
                    return "teEntityID";  // todo
                case "teUUID":
                    return "teUUID"; // todo
                case "teStructuredDataDateAndTime":
                    return "teStructuredDataDateAndTime";
                case "DBID":
                    return "DBID"; // todo
            }
            throw new NotImplementedException();
        }
    }
}