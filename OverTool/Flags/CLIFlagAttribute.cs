using System;

namespace OverTool.Flags {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
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
            if (@in.ToLower() == "true" || @in.ToLower() == "1" || @in.ToLower() == "y" || @in.ToLower() == "yes") {
                return true;
            }
            return false;
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
