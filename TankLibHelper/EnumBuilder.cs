using System.CodeDom.Compiler;
using System.IO;

namespace TankLibHelper {
    public class EnumBuilder : ClassBuilder {
        private readonly FieldNew _field;
        
        public override bool HasRealName => Info.KnownEnums.ContainsKey(Hash);

        public EnumBuilder(StructuredDataInfo info, FieldNew field) : base(info) {
            _field = field;
            Hash = _field.TypeHash2;
            Name = Info.GetEnumName(Hash);
        }

        public override IndentedTextWriter Build(FileWriter file) {
            IndentedTextWriter writer = new IndentedTextWriter(new StringWriter(), "    ");

            string attribute;

            //if (!Info.KnownEnums.ContainsKey(_hash)) {
                attribute = $"[STUEnum(0x{Hash:X8})]";
            //} else {
            //    attribute = $"[{nameof(STUEnumAttribute)}(0x{_hash:X8}, \"{Info.GetEnumName(_hash)}\")]";
            //}
            
            writer.WriteLine($"{attribute}");

            string type = GetSizeType(_field.m_size);
            writer.WriteLine($"public enum {Name} : {type}");
            writer.WriteLine("{");
            writer.Indent++;

            if (Info.Enums.TryGetValue(Hash, out var enumData)) {
                foreach (var value in enumData.m_values) {
                    attribute = $"[STUField(0x{value.Hash2:X8})]";
                    
                    var safeValue = value.GetSafeValue(_field);
                    if (safeValue > 0) {
                        writer.WriteLine($"{attribute} {Info.GetEnumValueName(value.Hash2)} = 0x{safeValue:X},");
                    } else {
                        writer.WriteLine($"{attribute} {Info.GetEnumValueName(value.Hash2)} = {safeValue},");
                    }
                }
            }

            writer.Indent--;
            writer.Write("}"); // close enum
            
            file.m_children.Add(writer);
            
            return writer;
        }

        private string GetSizeType(uint size) {
            switch (size) {
                default:
                    return null;
                case 8:
                    return "long";
                case 4:
                    return "int";
                case 2:
                    return "short";
                case 1:
                    return "byte";
            }
        }
    }
}