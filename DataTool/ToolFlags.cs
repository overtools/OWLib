using System;
using DataTool.Flag;
using JetBrains.Annotations;

namespace DataTool {
    [Serializable, UsedImplicitly]
    public class ToolFlags : ICLIFlags {
        [CLIFlag(Flag = "directory", Positional = 0, NeedsValue = true, Required = true, Help = "Overwatch Directory")]
        public string OverwatchDirectory;

        [CLIFlag(Flag = "mode", Positional = 1, NeedsValue = true, Required = true, Help = "Extraction Mode")]
        public string Mode;

        [CLIFlag(Default = false, Flag = "online", Help = "Allow downloading of corrupted files", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool Online;

        [CLIFlag(Default = null, Flag = "language", Help = "Language to load", NeedsValue = true, Valid = new[] {"deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW"})]
        [Alias("L")]
        public string Language;

        [CLIFlag(Default = null, Flag = "speech-language", Help = "Speech Language to load", NeedsValue = true, Valid = new[] {"deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW"})]
        [Alias("T")]
        public string SpeechLanguage;

        [CLIFlag(Default = false, Flag = "graceful-exit", Help = "When enabled don't crash on invalid CMF Encryption", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool GracefulExit;

        [CLIFlag(Default = true, Flag = "cache", Help = "Cache Index files from CDN", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool UseCache;

        [CLIFlag(Default = true, Flag = "cache-data", Help = "Cache Data files from CDN", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        // ReSharper disable once InconsistentNaming
        public bool CacheCDNData;

        [CLIFlag(Default = false, Flag = "validate-cache", Help = "Validate files from CDN", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool ValidateCache;

        [CLIFlag(Default = false, Flag = "quiet", Help = "Suppress majority of output messages", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        [Alias("q")]
        [Alias("silent")]
        public bool Quiet;

        [CLIFlag(Default = false, Flag = "string-guid", Help = "Returns all strings as their GUID instead of their value", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"}, Hidden = true)]
        public bool StringsAsGuids;

        [CLIFlag(Default = false, Flag = "skip-keys", Help = "Skip key detection", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipKeys;

        [CLIFlag(Default = false, Flag = "rcn", Help = "use (R)CN? CMF", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool RCN;

        // todo: maybe somebody should implement these
        // [CLIFlag(Flag = "force-replace-guid", Help = "Replace these GUIDs", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagGUIDDict" })]
        // public Dictionary<ulong, ulong> ForcedReplacements;
        //
        // [CLIFlag(Flag = "ignore-guid", Help = "Ignore these GUIDs", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagGUIDArray" })]
        // public List<ulong> IgnoreGUIDs;

        [CLIFlag(Default = false, Flag = "deduplicate-textures", Help = "Re-use textures from other models", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        [Alias("0")]
        public bool Deduplicate;

        [CLIFlag(Default = null, Flag = "scratchdb", NeedsValue = true, Help = "Directory for persistent database storage for deduplication info")]
        public string ScratchDBPath;

        [CLIFlag(Default = false, Flag = "args-save", Help = "Save current arguments", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        [Alias("args")]
        public bool SaveArgs;

        [CLIFlag(Default = false, Flag = "args-reset", Help = "Reset program arguments", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        [Alias("argr")]
        public bool ResetArgs;

        [CLIFlag(Default = false, Flag = "args-delete", Help = "Delete saved program arguments", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        [Alias("argd")]
        public bool DeleteArgs;

        [CLIFlag(Default = false, Flag = "no-names", Help = "Don't use names for textures", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool NoNames;

        [CLIFlag(Default = false, Flag = "canonical-names", Help = "Only use canonical names", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool OnlyCanonical;

        [CLIFlag(Default = false, Flag = "no-guid-names", Help = "Completely disables using GUIDNames", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool NoGuidNames;

        [CLIFlag(Default = false, Flag = "extract-shaders", Help = "Extract shader files", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool ExtractShaders;

        [CLIFlag(Default = false, Flag = "disable-language-registry", Help = "Disable fetching language from registry", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool NoLanguageRegistry;

        [CLIFlag(Default = false, Flag = "allow-manifest-fallback", Help = "Allows falling back to older versions if manfiest doesn't exist", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool TryManifestFallback;

        public override bool Validate() => true;
    }
}
