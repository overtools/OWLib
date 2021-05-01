using System;
using DataTool.Flag;

namespace DataTool.ToolLogic.Extract {
    [Serializable]
    public class ExtractFlags : ICLIFlags {
        [CLIFlag(Flag = "out-path", NeedsValue = true, Help = "Output path", Positional = 2, Required = true)]
        public string OutputPath;

        [CLIFlag(Default = "tif", NeedsValue = true, Flag = "convert-textures-type", Help = "Texture output type", Valid = new[] {"dds", "tif", "png", "jpg", "hdr"})]
        public string ConvertTexturesType;

        [CLIFlag(Default = false, Flag = "convert-lossless-textures", Help = "Output lossless textures (if converted)", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool ConvertTexturesLossless;

        [CLIFlag(Default = false, Flag = "raw-textures", Help = "Do not convert textures", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool RawTextures;

        [CLIFlag(Default = false, Flag = "raw-sound", Help = "Do not convert sounds", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool RawSound;

        [CLIFlag(Default = false, Flag = "raw-models", Help = "Do not convert models", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool RawModels;

        [CLIFlag(Default = false, Flag = "raw-animations", Help = "Do not convert animations", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool RawAnimations;

        [CLIFlag(Default = false, Flag = "skip-textures", Help = "Skip texture extraction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipTextures;

        [CLIFlag(Default = false, Flag = "skip-sound", Help = "Skip sound extraction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipSound;

        [CLIFlag(Default = false, Flag = "skip-models", Help = "Skip model extraction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipModels;

        [CLIFlag(Default = false, Flag = "skip-animations", Help = "Skip animation extraction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipAnimations;

        [CLIFlag(Default = false, Flag = "extract-refpose", Help = "Extract skeleton refposes", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool ExtractRefpose;

        [CLIFlag(Default = false, Flag = "extract-mstu", Help = "Extract model STU", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool ExtractModelStu;

        [CLIFlag(Default = false, Flag = "raw", Help = "Skip all conversion", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool Raw;

        [CLIFlag(Default = (byte) 1, NeedsValue = true, Flag = "lod", Help = "Force extracted model LOD", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagByte"})]
        public byte LOD;

        [CLIFlag(Default = false, Flag = "scale-anims", Help = "set to true for Blender 2.79, false for Maya and when Blender SEAnim tools are updated for 2.8", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool ScaleAnims;

        [CLIFlag(Default = false, Flag = "flatten", Help = "Flatten directory structure", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool FlattenDirectory;

        [CLIFlag(Default = false, Flag = "force-dds-multisurface", Help = "Save multisurface textures as DDS", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool ForceDDSMultiSurface;

        [CLIFlag(Default = false, Flag = "sheet-multisurface", Help = "Save multisurface textures as one large image, tiled across in the Y (vertical) direction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SheetMultiSurface;

        [CLIFlag(Default = false, Flag = "extract-mips", Help = "Extract mip files", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SaveMips;

        [CLIFlag(Default = true, Flag = "subtitles-with-sounds", Help = "Extract subtitles alongside voicelines", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SubtitlesWithSounds;

        [CLIFlag(Default = false, Flag = "voice-group-by-hero", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool VoiceGroupByHero;

        [CLIFlag(Default = true, Flag = "voice-group-by-type", Hidden = true, Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool VoiceGroupByType;

        [CLIFlag(Default = false, Flag = "skip-map-env-sound", Help = "Skip map Environment sound extraction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipMapEnvironmentSound;

        [CLIFlag(Default = false, Flag = "skip-map-env-lut", Help = "Skip map Environment lut extraction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipMapEnvironmentLUT;

        [CLIFlag(Default = false, Flag = "skip-map-env-blend", Help = "Skip map Environment blend cubemap extraction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipMapEnvironmentBlendCubemap;

        [CLIFlag(Default = false, Flag = "skip-map-env-ground", Help = "Skip map Environment ground cubemap extraction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipMapEnvironmentGroundCubemap;

        [CLIFlag(Default = false, Flag = "skip-map-env-sky", Help = "Skip map Environment sky cubemap extraction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipMapEnvironmentSkyCubemap;

        [CLIFlag(Default = false, Flag = "skip-map-env-skybox", Help = "Skip map Environment skybox extraction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipMapEnvironmentSkybox;

        [CLIFlag(Default = false, Flag = "skip-map-env-entity", Help = "Skip map Environment entity extraction", Parser = new[] {"DataTool.Flag.Converter", "CLIFlagBoolean"})]
        public bool SkipMapEnvironmentEntity;

        // [CLIFlag(Default = false, Flag = "convert-bnk", Help = "Convert .bnk files to .wem", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        // public bool ConvertBnk;

        public override bool Validate() => true;
    }
}
