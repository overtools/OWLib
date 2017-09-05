namespace DataTool.Flag {
    public static class Converter {
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
    }
}