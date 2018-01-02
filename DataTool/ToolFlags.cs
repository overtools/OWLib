using DataTool.Flag;

namespace DataTool {
    public class ToolFlags : ICLIFlags {
        [CLIFlag(Flag = "directory", Positional = 0, Help = "Overwatch Directory", Required = true)]
        public string OverwatchDirectory;
        
        [CLIFlag(Flag = "mode", Positional = 1, Help = "Extraction Mode", Required = true)]
        public string Mode;

        [CLIFlag(Default = "enUS", Flag = "language", Help = "Language to load", NeedsValue = true, Valid = new[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW" })]
        [Alias(Alias = "L")]
        [Alias(Alias = "lang")]
        public string Language;

        [CLIFlag(Default = false, Flag = "graceful-exit", Help = "When enabled don't crash on invalid CMF Encryption", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool GracefulExit;

        [CLIFlag(Default = true, Flag = "cache", Help = "Cache Index files from CDN", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBooleanInv" })]
        public bool UseCache;

        [CLIFlag(Default = true, Flag = "cache-data", Help = "Cache Data files from CDN", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBooleanInv" })]
        public bool CacheData;

        [CLIFlag(Default = false, Flag = "validate-cache", Help = "Validate files from CDN", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ValidateCache;
        
        [CLIFlag(Default = false, Flag = "quiet", Help = "Suppress majority of output messages", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        [Alias(Alias = "q")]
        [Alias(Alias = "silent")]
        public bool Quiet;

        [CLIFlag(Default = false, Flag = "skip-keys", Help = "Skip key detection", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        [Alias(Alias = "n")]
        public bool SkipKeys;

        [CLIFlag(Default = false, Flag = "expert", Help = "Output more asset information", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        [Alias(Alias = "ex")]
        public bool Expert;

        [CLIFlag(Default = false, Flag = "rcn", Help = "use (R)CN? CMF", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool RCN;

        // [CLIFlag(Default = true, Flag = "threads", Help = "Use multiple threads", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool Threads = false;  // disabled for now because it's not great

        public override bool Validate() => true;
    }
}
