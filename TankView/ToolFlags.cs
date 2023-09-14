using DataTool.Flag;

namespace TankView {
    public class ToolFlags : ICLIFlags {
        [CLIFlag(Default = null, Flag = "language", Help = "Language to load", NeedsValue = true, Valid = new[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "thTH", "trTR", "zhCN", "zhTW" })]
        [Alias("L")]
        [Alias("lang")]
        public string Language;

        [CLIFlag(Default = null, Flag = "speech-language", Help = "Speech Language to load", NeedsValue = true, Valid = new[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "thTH", "trTR", "zhCN", "zhTW" })]
        [Alias("T")]
        [Alias("speechlang")]
        public string SpeechLanguage;

        [CLIFlag(Default = true, Flag = "online", Help = "Allow downloading of corrupted files", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool Online;

        public override bool Validate() => true;
    }
}
