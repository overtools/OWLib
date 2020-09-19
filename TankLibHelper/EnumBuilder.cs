using System.CodeDom.Compiler;
using System.IO;

namespace TankLibHelper {
    public class EnumBuilder : ClassBuilder {
        private uint _hash => _field.TypeHash2;
        private readonly FieldNew _field;
        
        public override bool HasRealName => Info.KnownEnums.ContainsKey(_hash);

        public EnumBuilder(StructuredDataInfo info, FieldNew field) : base(info) {
            _field = field;
            Name = Info.GetEnumName(_hash);
        }

        public override IndentedTextWriter Build(FileWriter file) {
            IndentedTextWriter writer = new IndentedTextWriter(new StringWriter(), "    ");

            string attribute;

            //if (!Info.KnownEnums.ContainsKey(_hash)) {
                attribute = $"[STUEnum(0x{_hash:X8})]";
            //} else {
            //    attribute = $"[{nameof(STUEnumAttribute)}(0x{_hash:X8}, \"{Info.GetEnumName(_hash)}\")]";
            //}
            
            writer.WriteLine($"{attribute}");

            string type = GetSizeType(_field.m_size);
            writer.WriteLine($"public enum {Name} : {type}");
            writer.WriteLine("{");
            writer.Indent++;

            if (Info.Enums.ContainsKey(_hash)) {
                var enumData = Info.Enums[_hash];
                foreach (var value in enumData.m_values) {
                    //if (!Info.KnownEnumNames.ContainsKey(_hash)) {
                        attribute = $"[STUField(0x{value.Hash2:X8})]";
                    //} else {
                    //    attribute = $"[STUField(0x{value.Hash2:X8}, \"{Info.GetEnumValueName(value.Hash2)}\")]";
                    //}

                    var safeValue = value.m_value;
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (_field.m_size) {
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
                    
                    writer.WriteLine($"{attribute} {Info.GetEnumValueName(value.Hash2)} = 0x{safeValue:X},");
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