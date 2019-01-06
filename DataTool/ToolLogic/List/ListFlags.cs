using System;
using DataTool.Flag;

namespace DataTool.ToolLogic.List {
    [Serializable]
    public class ListFlags : ICLIFlags {
        [CLIFlag(Default = false, Flag = "json", Help = "Output JSON to stderr", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool JSON;
        
        [CLIFlag(Flag = "out", Help = "Output JSON file")]
        [Alias("o")]
        public string Output;

        public override bool Validate() => true;
    }
}