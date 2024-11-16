using System;
using DataTool.Flag;
using JetBrains.Annotations;

namespace DataTool.ToolLogic.Extract {
    [Serializable, UsedImplicitly]
    [FlagInfo(Name = "Extract", Description = "Flags for extracting data. These apply to all extract-* modes.")]
    public class ExtractFlags : IToolFlags {
        [CLIFlag(Flag = "out-path", NeedsValue = true, Help = "Output path to save data", Positional = 2, Required = true)]
        public string OutputPath;

        [CLIFlag(Default = "tif", NeedsValue = true, Flag = "convert-textures-type", Help = "Texture output type", Valid = new[] {"dds", "tif", "png"})]
        public string ConvertTexturesType;

        [CLIFlag(Default = "owanimclip", NeedsValue = true, Flag = "convert-animations-type", Help = "Animation output type", Valid = new[] {"owanimclip", "seanim"})]
        public string ConvertAnimationsType;

        [CLIFlag(Default = false, Flag = "convert-lossless-textures", Help = "Output lossless textures (if converted)", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool ConvertTexturesLossless;

        [CLIFlag(Default = false, Flag = "extract-refpose", Help = "Extract skeleton refposes", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool ExtractRefpose;

        [CLIFlag(Default = false, Flag = "raw-textures", Help = "Do not convert textures", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool RawTextures;

        [CLIFlag(Default = false, Flag = "raw-sound", Help = "Do not convert sounds", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool RawSound;

        [CLIFlag(Default = false, Flag = "raw-models", Help = "Do not convert models", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool RawModels;

        [CLIFlag(Default = false, Flag = "raw-animations", Help = "Do not convert animations", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool RawAnimations;

        [CLIFlag(Default = false, Flag = "raw", Help = "Skip all conversion", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool Raw;

        [CLIFlag(Default = false, Flag = "skip-textures", Help = "Skip texture extraction", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipTextures;

        [CLIFlag(Default = false, Flag = "skip-sound", Help = "Skip sound extraction", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipSound;

        [CLIFlag(Default = false, Flag = "skip-models", Help = "Skip model extraction", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipModels;

        [CLIFlag(Default = false, Flag = "skip-animations", Help = "Skip animation extraction", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipAnimations;

        [CLIFlag(Default = false, Flag = "skip-animation-effects", Help = "Skip animation effect extraction", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipAnimationEffects;

        [CLIFlag(Default = false, Flag = "scale-anims", Hidden = true, Help = "set to true for Blender 2.79, false for Maya and when Blender SEAnim tools are updated for 2.8", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool ScaleAnims;

        [CLIFlag(Default = false, Flag = "flatten", Help = "Flatten directory structure", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool FlattenDirectory;

        [CLIFlag(Default = false, Flag = "force-dds-multisurface", Help = "Save multisurface textures as DDS", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool ForceDDSMultiSurface;

        [CLIFlag(Default = false, Flag = "sheet-multisurface", Help = "Save multisurface textures as one large image, tiled across in the Y (vertical) direction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SheetMultiSurface;

        [CLIFlag(Default = false, Flag = "combine-multisurface", Help = "Combine all surfaces into one image (only supported on TIF and DDS)", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool CombineMultiSurface;

        [CLIFlag(Default = false, Flag = "grayscale", Help = "Convert single channel textures to grayscale RGB", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool Grayscale;

        [CLIFlag(Default = false, Flag = "extract-mips", Help = "Extract mip files", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SaveMips;

        [CLIFlag(Default = false, Flag = "subtitles-with-sounds", Help = "Extract subtitles alongside voicelines", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SubtitlesWithSounds;

        [CLIFlag(Default = true, Flag = "subtitles-as-sounds", Help = "Saves the sound files as the subtitle", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SubtitlesAsSound;

        [CLIFlag(Default = true, Flag = "voice-group-by-hero", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool VoiceGroupByHero;

        [CLIFlag(Default = true, Flag = "voice-group-by-type", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool VoiceGroupByType;

        [CLIFlag(Default = false, Flag = "voice-group-by-skin", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool VoiceGroupBySkin;

        [CLIFlag(Default = false, Flag = "voice-group-by-locale", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool VoiceGroupByLocale;

        [CLIFlag(Default = false, Flag = "voice-03f-in-type", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool VoiceGroup03FInType;

        [CLIFlag(Default = false, Flag = "xml", Help = "Convert STUs to xml when extracted with ExtractDebugType", Hidden = true, Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool ConvertToXML;

        [CLIFlag(Default = false, Flag = "keep-channels", Help = "Keep all audio channels when converting Ogg Opus", Hidden = true, Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool KeepSoundChannels;

        [CLIFlag(Default = false, Flag = "use-texture-decoder", Help = "Use TextureDecoder for decoding textures, slower but more accurate (enforced on Linux)", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool UseTextureDecoder;

        [CLIFlag(Default = false, Flag = "all-lods", Help = "Extract all model LODs", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        [Alias("lods")]
        public bool AllLODs;

        public override bool Validate() => true;

        public void EnsureOutputDirectory() {
            if (string.IsNullOrEmpty(OutputPath)) {
                throw new InvalidOperationException("no output path");
            }
        }
    }
}