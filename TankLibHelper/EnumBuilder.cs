using System.Globalization;
using System.Text;

namespace TankLibHelper {
    public class EnumBuilder : ClassBuilder {
        private readonly uint _hash;
        private readonly STUFieldJSON _field;
        
        public EnumBuilder(BuilderConfig config, StructuredDataInfo info, STUFieldJSON field) : base(config, info) {
            _hash = uint.Parse(field.Type, NumberStyles.HexNumber);
            _field = field;
            
            Name = Info.GetEnumName(_hash);
        }

        public uint GetHash() {
            return _hash;
        }

        public static bool IsValid(STUFieldJSON field) {
            return field.SerializationType == 8 || field.SerializationType == 9;
        }

        public override string BuildCSharp() {
            StringBuilder builder = new StringBuilder();
            
            WriteDefaultHeader(builder, "Enum", "TankLibHelper.EnumBuilder");

            string type = GetSizeType(_field.Size);
            builder.AppendLine($"    public enum {Name} : {type} {{");
            
            
            builder.AppendLine("    }");  // close enum
            builder.AppendLine("}");  // close namespace

            return builder.ToString();
        }

        private string GetSizeType(int size) {
            switch (size) {
                default:
                    return null;
                case 8:
                    return "ulong";
                case 4:
                    return "uint";
                case 2:
                    return "ushort";
                case 1:
                    return "byte";
            }
        }
    }
}