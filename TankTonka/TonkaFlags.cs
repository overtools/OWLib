using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DataTool.Flag;

namespace TankTonka {
    public class TonkaFlags : ICLIFlags {
        [CLIFlag(Flag = "directory", Positional = 0, Help = "Overwatch Directory", Required = true)]
        public string OverwatchDirectory;
        
        [CLIFlag(Default = null, Flag = "language", Help = "Language to load", NeedsValue = true, Valid = new[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW" })]
        [Alias(Alias = "L")]
        [Alias(Alias = "lang")]
        public string Language;

        [CLIFlag(Default = null, Flag = "speech-language", Help = "Speech Language to load", NeedsValue = true, Valid = new[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW" })]
        [Alias(Alias = "T")]
        [Alias(Alias = "speechlang")]
        public string SpeechLanguage;
        
        public override bool Validate() => true;
        
        public static class Converter {
            public static object CLIFlagTypeArray(List<string> @in) {
                return @in.Select(x => ushort.Parse(x, NumberStyles.HexNumber)).ToList();
            }
        }
    }
    
    // todo: i cba to make this work. maybe in the future or whatever
}