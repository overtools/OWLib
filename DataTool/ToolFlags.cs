using System;
using DataTool.Flag;
using JetBrains.Annotations;

namespace DataTool {
    [Serializable, UsedImplicitly]
    public class ToolFlags : IToolFlags {
        [CLIFlag(Flag = "h", Default = false, Help = "Print this help text", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        [Alias("help")]
        public bool Help;

        [CLIFlag(Default = null, Flag = "language", Help = "Language to load", NeedsValue = true, Valid = new[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "thTH", "trTR", "zhCN", "zhTW" })]
        [Alias("L")]
        public string Language;

        [CLIFlag(Default = null, Flag = "speech-language", Help = "Speech Language to load", NeedsValue = true, Valid = new[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "thTH", "trTR", "zhCN", "zhTW" })]
        [Alias("T")]
        public string SpeechLanguage;

        [CLIFlag(Default = true, Flag = "online", Help = "Allow downloading of corrupted files", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool Online;

        [CLIFlag(Default = false, Flag = "quiet", Help = "Suppress majority of output messages", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        [Alias("q")]
        public bool Quiet;

        [CLIFlag(Default = false, Flag = "deduplicate-textures", Help = "Re-use textures from other models", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        [Alias("0")]
        public bool Deduplicate;

        [CLIFlag(Default = false, Flag = "string-guid", Help = "Returns all strings as their GUID instead of their value", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"}, Hidden = true)]
        public bool StringsAsGuids;

        [CLIFlag(Default = false, Flag = "rcn", Help = "use (R)CN? CMF", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool RCN;

        [CLIFlag(Default = null, Flag = "scratchdb", Hidden = true, NeedsValue = true, Help = "Directory for persistent database storage for deduplication info")]
        public string ScratchDBPath;

        [CLIFlag(Default = false, Flag = "no-names", Help = "Don't use names for textures", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool NoNames;

        [CLIFlag(Default = false, Flag = "canonical-names", Help = "Only use canonical names", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool OnlyCanonical;

        [CLIFlag(Default = false, Flag = "no-guid-names", Help = "Completely disables using GUIDNames", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool NoGuidNames;

        [CLIFlag(Default = false, Flag = "extract-shaders", Help = "Extract shader files", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool ExtractShaders;

        [CLIFlag(Default = false, Flag = "disable-language-registry", Help = "Disable fetching language from registry", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool NoLanguageRegistry;

        [CLIFlag(Default = false, Flag = "args-save", Help = "Save current arguments", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        [Alias("args")]
        public bool SaveArgs;

        [CLIFlag(Default = false, Flag = "args-reset", Help = "Reset program arguments", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        [Alias("argr")]
        public bool ResetArgs;

        [CLIFlag(Default = false, Flag = "args-delete", Help = "Delete saved program arguments", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        [Alias("argd")]
        public bool DeleteArgs;

        [CLIFlag(Default = false, Flag = "debug", Help = "Enable debug logging", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool Debug;

        public override bool Validate() => true;
    }
}
