using System.Collections.Generic;
using System.Text;

namespace STUHashTool {
    public class EnumBuilder {
        public STUEnumData EnumData;
        
        public EnumBuilder(STUEnumData enumData) {
            EnumData = enumData;
        }

        public string Build(Dictionary<uint, string> enumNames) {
            StringBuilder sb = new StringBuilder();

            string name = $"STUEnum_{EnumData.Checksum:X8}";
            string attrDef = $"[STUEnum(0x{EnumData.Checksum:X8})]";
            if (enumNames.ContainsKey(EnumData.Checksum)) {
                name = enumNames[EnumData.Checksum];
                attrDef = $"[STUEnum(0x{EnumData.Checksum:X8}, \"{name}\")]";
            }

            sb.AppendLine("namespace STULib.Types.Enums {");
            sb.AppendLine($"    {attrDef}");
            sb.AppendLine($"    public enum {name} : {EnumData.Type} {{");
            sb.AppendLine("    }");
            sb.Append("}");
            
            return sb.ToString();
        }
    }
}