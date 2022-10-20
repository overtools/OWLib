using System;

namespace DataTool.Flag {
    [Serializable]
    public abstract class ICLIFlags {
        [CLIFlag(AllPositionals = true)]
        public string[] Positionals;

        public abstract bool Validate();
    }

    public abstract class IToolFlags : ICLIFlags {
        [CLIFlag(Flag = "directory", Positional = 0, NeedsValue = true, Required = true, Help = "Overwatch Install Directory")]
        public string OverwatchDirectory;

        [CLIFlag(Flag = "mode", Positional = 1, NeedsValue = true, Required = true, Help = "Extraction Mode")]
        public string Mode;

        public abstract override bool Validate();
    }
}