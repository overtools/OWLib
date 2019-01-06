using System;
using DataTool.Flag;

namespace DataTool.ToolLogic.Render {
    [Serializable]
    public class RenderFlags : ICLIFlags {
        [CLIFlag(Flag = "out-path", NeedsValue = true, Help = "Output path", Positional = 2, Required = true)]
        public string OutputPath;
        
        [CLIFlag(Default = 1920, Flag = "width", Help = "Screen Width", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagInt"})]
        [Alias("w")]
        public int Width;

        [CLIFlag(Default = 1080, Flag = "height", Help = "Screen Height", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagInt"})]
        [Alias("H")]
        public int Height;

        public override bool Validate() => true;
    }
}
