using System.Collections.Generic;
using System.Text;

namespace STUHashTool {
    public class EnumBuilder {
        public STUEnumData EnumData;
        
        public EnumBuilder(STUEnumData enumData) {
            EnumData = enumData;
        }

        public string Build(Dictionary<uint, string> enumNames, string enumNamespace="STULib.Types.Enums", bool properTypePaths=false) {
            StringBuilder sb = new StringBuilder();

            string enumTypeDef = properTypePaths ? "STULib.STUEnum" : "STUEnum";
            string name = $"STUEnum_{EnumData.Checksum:X8}";
            string attrDef = $"[{enumTypeDef}(0x{EnumData.Checksum:X8})]";
            if (enumNames.ContainsKey(EnumData.Checksum)) {
                name = enumNames[EnumData.Checksum];
                attrDef = $"[{enumTypeDef}(0x{EnumData.Checksum:X8}, \"{name}\")]";
            }

            sb.AppendLine($"namespace {enumNamespace} {{");
            sb.AppendLine($"    {attrDef}");
            sb.AppendLine($"    public enum {name} : {EnumData.Type} {{");
            sb.AppendLine("    }");
            sb.Append("}");
            
            return sb.ToString();
        }
    }
}