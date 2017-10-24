using DataTool.Flag;

namespace DataTool.ToolLogic.Extract {
    public class ExtractFlags : ICLIFlags {
        [CLIFlag(Help = "Output path", Positional = 2, Required = true)]
        public string OutputPath;
        
        [CLIFlag(Default = true, Flag = "convert-textures", Help = "Convert .004 files to .dds", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ConvertTextures;
        
        [CLIFlag(Default = true, Flag = "convert-wem", Help = "Convert .wem files to .ogg", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ConvertWem;
        
        // [CLIFlag(Default = false, Flag = "convert-bnk", Help = "Convert .bnk files to .wem", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        // public bool ConvertBnk;
        
        [CLIFlag(Default = false, Flag = "skip-audio", Help = "Skip audio extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipAudio;
        
        [CLIFlag(Default = false, Flag = "skip-textures", Help = "Skip texture extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipTextures;
        
        // [CLIFlag(Flag = "out", Help = "Output JSON file")]
        // [Alias(Alias = "o")]
        // public string Output;

        public override bool Validate() => true;
    }
}