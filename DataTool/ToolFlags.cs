using DataTool.Flag;

namespace DataTool {
    public class ToolFlags : ICLIFlags {
        [CLIFlag(Flag = "directory", Positional = 0, Help = "Overwatch Directory")]
        public string OverwatchDirectory;
        
        [CLIFlag(Flag = "mode", Positional = 1, Help = "Extraction Mode")]
        public string Mode;
        
        [CLIFlag(Default = null, Flag = "language", Help = "Language to load", NeedsValue = true, Valid = new[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW" })]
        [Alias("L")]
        [Alias("lang")]
        public string Language;

        [CLIFlag(Default = null, Flag = "speech-language", Help = "Speech Language to load", NeedsValue = true, Valid = new[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW" })]
        [Alias("T")]
        [Alias("speechlang")]
        public string SpeechLanguage;

        [CLIFlag(Default = false, Flag = "graceful-exit", Help = "When enabled don't crash on invalid CMF Encryption", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool GracefulExit;

        [CLIFlag(Default = true, Flag = "cache", Help = "Cache Index files from CDN", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBooleanInv" })]
        public bool UseCache;

        [CLIFlag(Default = true, Flag = "cache-data", Help = "Cache Data files from CDN", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBooleanInv" })]
        public bool CacheCDNData;

        [CLIFlag(Default = false, Flag = "validate-cache", Help = "Validate files from CDN", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ValidateCache;
        
        [CLIFlag(Default = false, Flag = "quiet", Help = "Suppress majority of output messages", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        [Alias("q")]
        [Alias("silent")]
        public bool Quiet;

        [CLIFlag(Default = false, Flag = "skip-keys", Help = "Skip key detection", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        [Alias("n")]
        public bool SkipKeys;

        [CLIFlag(Default = false, Flag = "expert", Help = "Output more asset information", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        [Alias("ex")]
        public bool Expert;

        [CLIFlag(Default = false, Flag = "rcn", Help = "use (R)CN? CMF", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        // ReSharper disable once InconsistentNaming
        public bool RCN;

        // todo: maybe somebody should implement these
        // [CLIFlag(Flag = "force-replace-guid", Help = "Replace these GUIDs", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagGUIDDict" })]
        // public Dictionary<ulong, ulong> ForcedReplacements;
        //
        // [CLIFlag(Flag = "ignore-guid", Help = "Ignore these GUIDs", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagGUIDArray" })]
        // public List<ulong> IgnoreGUIDs;

        [CLIFlag(Default = false, Flag = "deduplicate-textures", Help = "Re-use textures from other models", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        [Alias("0")]
        public bool Deduplicate;

        [CLIFlag(Default = null, Flag = "scratchdb", NeedsValue = true, Help = "Directory for persistent database storage for deduplication info")]
        public string ScratchDBPath;

        [CLIFlag(Default = false, Flag = "args-save", Help = "Save current arguments", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        [Alias("args")]
        public bool SaveArgs;

        [CLIFlag(Default = false, Flag = "args-reset", Help = "Reset program arguments", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        [Alias("argr")]
        public bool ResetArgs;

        [CLIFlag(Default = false, Flag = "args-delete", Help = "Delete saved program arguments", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        [Alias("argd")]
        public bool DeleteArgs;

        public override bool Validate() => true;
    }
}
