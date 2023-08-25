using System;

namespace DataTool.Flag {
    [Serializable]
    public abstract class ICLIFlags {
        [CLIFlag(AllPositionals = true)]
        public string[] Positionals;

        public abstract bool Validate();
    }

    public abstract class ILocaleFlags : ICLIFlags {
        [CLIFlag(Default = null, Flag = "language", Help = "Language to load", NeedsValue = true, Valid = new[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "thTH", "trTR", "zhCN", "zhTW" })]
        [Alias("L")]
        public string Language;

        [CLIFlag(Default = null, Flag = "speech-language", Help = "Speech Language to load", NeedsValue = true, Valid = new[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "thTH", "trTR", "zhCN", "zhTW" })]
        [Alias("T")]
        public string SpeechLanguage;

        public abstract override bool Validate();
    }

    public abstract class IToolFlags : ILocaleFlags {
        [CLIFlag(Flag = "directory", Positional = 0, NeedsValue = true, Required = true, Help = "Overwatch Install Directory")]
        public string OverwatchDirectory;

        [CLIFlag(Flag = "mode", Positional = 1, NeedsValue = true, Required = true, Help = "Extraction Mode")]
        public string Mode;

        public abstract override bool Validate();
    }
}