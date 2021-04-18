using System;
using DataTool.Flag;
using JetBrains.Annotations;

namespace DataTool.ToolLogic.List {
    [Serializable, UsedImplicitly]
    public class ListFlags : ICLIFlags {
        [CLIFlag(Default = false, Flag = "json", Help = "Output JSON to stderr", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool JSON;

        [CLIFlag(Flag = "out", NeedsValue = true, Help = "Output JSON file")]
        [Alias("o")]
        public string Output;

        [CLIFlag(Default = false, Flag = "flatten", Help = "Flatten output", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool Flatten;

        [CLIFlag(Default = false, Flag = "simplify", Help = "Reduces the amount of information output by -list commands (doesn't work for JSON out)", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool Simplify;

        public override bool Validate() => true;
    }
}
