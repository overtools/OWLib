using System;

namespace DataTool.Flag {
    [Serializable]
    public abstract class ICLIFlags {
        [CLIFlag(AllPositionals = true)]
        public string[] Positionals;

        [CLIFlag(Flag = "h", Default = false, Help = "Print this help text", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        [Alias("help")]
        public bool Help;

        public abstract bool Validate();
    }
}
