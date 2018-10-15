using System.Globalization;
using System.Text;
using TankLib.STU;

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

            string attribute;

            if (!Info.KnownEnums.ContainsKey(_hash)) {
                attribute = $"[{nameof(STUEnumAttribute)}(0x{_hash:X8})]";
            } else {
                attribute = $"[{nameof(STUEnumAttribute)}(0x{_hash:X8}, \"{Info.GetEnumName(_hash)}\")]";
            }
            
            builder.AppendLine($"    {attribute}");

            string type = GetSizeType(_field.Size);
            builder.AppendLine($"    public enum {Name} : {type} {{");

            if (Info.Enums.ContainsKey(_hash)) {
                var enumData = Info.Enums[_hash];
                foreach (var @enum in enumData.Values) {
                    if (!Info.KnownEnumNames.ContainsKey(_hash)) {
                        attribute = $"[{nameof(STUFieldAttribute)}(0x{@enum.Hash:X8})]";
                    } else {
                        attribute = $"[{nameof(STUFieldAttribute)}(0x{@enum.Hash:X8}, \"{Info.GetEnumValueName(@enum.Hash)}\")]";
                    }

                    var safeValue = @enum.Value;
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (_field.Size) {
                        case 1:
                            safeValue = (byte)(safeValue % 0xFF);
                            break;
                        case 2:
                            safeValue = (ushort)(safeValue % 0xFFFFF);
                            break;
                        case 4:
                            safeValue = (uint)(safeValue % 0xFFFFFFFFFF);
                            break;
                    }

                    builder.AppendLine($"        {attribute}");
                    builder.AppendLine($"        {Info.GetEnumValueName(@enum.Hash)} = 0x{safeValue:X},");
                }
            }
            
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