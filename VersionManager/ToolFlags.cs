using DataTool.Flag;

namespace VersionManager {
    public class ToolFlags : ICLIFlags {
        [CLIFlag(Flag = "directory", Positional = 0, Help = "Overwatch Directory", Required = true)]
        public string OverwatchDirectory;

        [CLIFlag(Flag = "mode", Positional = 1, Help = "Mode", Required = true)]
        public string Mode;
        
        [CLIFlag(Default = "enUS", Flag = "language", Help = "Language to load", NeedsValue = true, Valid = new[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW" })]
        [Alias(Alias = "L")]
        [Alias(Alias = "lang")]
        public string Language;

        public override bool Validate() => true;
    }

    public class CreateFlags : ICLIFlags {
        public override bool Validate() => true;
    }

    public class ExtractFilesFlags : ICLIFlags {
        public override bool Validate() => true;
    }
}