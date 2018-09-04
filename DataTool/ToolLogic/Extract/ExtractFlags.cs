using DataTool.Flag;

namespace DataTool.ToolLogic.Extract {
    public class ExtractFlags : ICLIFlags {
        [CLIFlag(Help = "Output path", Positional = 2, Required = true)]
        public string OutputPath;
        
        [CLIFlag(Default = "tif", Flag = "convert-textures-type", Help = "Texture output type", Valid = new[] { "dds", "tif", "tga", "png", "jpg", "hdr" })]
        public string ConvertTexturesType;
        
        [CLIFlag(Default = false, Flag = "convert-lossless-textures", Help = "Output lossless textures (if converted)", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ConvertTexturesLossless;
        
        [CLIFlag(Default = true, Flag = "convert-textures", Help = "Convert .004 files to {convert-textures-type}", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ConvertTextures;
        
        [CLIFlag(Default = true, Flag = "convert-sound", Help = "Convert .wem files to .ogg", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ConvertSound;
        
        [CLIFlag(Default = true, Flag = "convert-models", Help = "Convert .00C files to .owmdl", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ConvertModels;
        
        [CLIFlag(Default = true, Flag = "convert-animations", Help = "Convert .006 files to .seanim", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ConvertAnimations;
        
        [CLIFlag(Default = false, Flag = "skip-textures", Help = "Skip texture extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipTextures;
        
        [CLIFlag(Default = false, Flag = "skip-sound", Help = "Skip sound extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipSound;
        
        [CLIFlag(Default = false, Flag = "skip-models", Help = "Skip model extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipModels;
        
        [CLIFlag(Default = false, Flag = "skip-animations", Help = "Skip animation extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipAnimations;
        
        [CLIFlag(Default = false, Flag = "extract-refpose", Help = "Extract skeleton refposes", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ExtractRefpose;
        
        [CLIFlag(Default = false, Flag = "raw", Help = "Skip all conversion", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool Raw;

        [CLIFlag(Default = (byte)1, Flag = "lod", Help = "Force extracted model LOD", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagByte" })]
        public byte LOD;
        
        [CLIFlag(Default = true, Flag = "spc-spray", Help = "", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBooleanInv" })]
        public bool SprayOnlyImage;
        
        // [CLIFlag(Default = false, Flag = "convert-bnk", Help = "Convert .bnk files to .wem", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        // public bool ConvertBnk;

        public override bool Validate() => true;
    }
}