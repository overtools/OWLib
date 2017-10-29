using DataTool.Flag;

namespace DataTool.ToolLogic.Extract {
    public class ExtractFlags : ICLIFlags {
        [CLIFlag(Help = "Output path", Positional = 2, Required = true)]
        public string OutputPath;
        
        [CLIFlag(Default = true, Flag = "convert-textures", Help = "Convert .004 files to .dds", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ConvertTextures;
        
        [CLIFlag(Default = true, Flag = "convert-sound", Help = "Convert .wem files to .ogg", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ConvertSound;
        
        [CLIFlag(Default = true, Flag = "convert-models", Help = "Convert .wem files to .ogg", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ConvertModels;
        
        [CLIFlag(Default = true, Flag = "convert-animations", Help = "Convert .wem files to .ogg", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ConvertAnimations;
        
        
        [CLIFlag(Default = false, Flag = "skip-textures", Help = "Skip texture extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipTextures;
        
        [CLIFlag(Default = false, Flag = "skip-sound", Help = "Skip audio extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipSound;
        
        [CLIFlag(Default = false, Flag = "skip-models", Help = "Skip model extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipModels;
        
        [CLIFlag(Default = false, Flag = "skip-animations", Help = "Skip animation extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipAnimations;
        
        [CLIFlag(Default = false, Flag = "raw", Help = "Skip all conversion", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool Raw;
        
        // [CLIFlag(Default = false, Flag = "convert-bnk", Help = "Convert .bnk files to .wem", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        // public bool ConvertBnk;

        public override bool Validate() => true;
    }
}