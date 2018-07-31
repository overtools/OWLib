using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TankLib;

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
        
        public static object CLIFlagByte(string @in) {
            return byte.Parse(@in);
        }
        
        public static object CLIFlagChar(string @in) {
            if (@in.Length == 0) {
                return (char)0;
            }
            return @in[0];
        }

        public static object CLIFlagTankGUID(string @in) {
            return new teResourceGUID((ulong)CLIFlagGUID(@in));
        }

        public static object CLIFlagTankGUIDDict(Dictionary<string, string> @in) {
            return @in.ToDictionary(x => (teResourceGUID)CLIFlagTankGUID(x.Key), x => (teResourceGUID)CLIFlagTankGUID(x.Value));
        }

        public static object CLIFlagTankGUIDArray(List<string> @in) {
            return @in.Select(x => (teResourceGUID)CLIFlagTankGUID(x)).ToList();
        }

        public static object CLIFlagGUID(string @in) {
            if (@in.StartsWith("0x")) {
                return ulong.Parse(@in.Substring(2), NumberStyles.HexNumber);
            } else {
                return ulong.Parse(@in);
            }
        }

        public static object CLIFlagGUIDDict(Dictionary<string, string> @in) {
            return @in.ToDictionary(x => (ulong)CLIFlagGUID(x.Key), x => (ulong)CLIFlagGUID(x.Value));
        }

        public static object CLIFlagGUIDArray(List<string> @in) {
            return @in.Select(x => (ulong)CLIFlagGUID(x)).ToList();
        }
    }
}