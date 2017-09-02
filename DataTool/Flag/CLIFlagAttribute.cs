using System;

namespace DataTool.Flag {
    [AttributeUsage(AttributeTargets.Field)]
    public class CLIFlagAttribute : Attribute {
        public string Flag = null;
        public string Help = null;
        public bool Required = false;
        public int Positional = -1;
        public object Default = null;
        public bool NeedsValue = false;
        public string[] Parser = null;
        public string[] Valid = null;

        public static object CLIFlagBoolean(string @in) {
            return @in.ToLower() == "true" || @in.ToLower() == "1" || @in.ToLower() == "y" || @in.ToLower() == "yes";
        }

        public static object CLIFlagBooleanInv(string @in) {
            return !(bool)CLIFlagBoolean(@in);
        }
        
        public static object CLIFlagInt(string @in) {
            return int.Parse(@in);
        }

        public static object CLIFlagChar(string @in) {
            if (@in.Length == 0) {
                return (char)0;
            }
            return @in[0];
        }

        public new string ToString() {
            return Flag;
        }
    }
}
