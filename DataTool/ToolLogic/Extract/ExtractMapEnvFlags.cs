using DataTool.Flag;

namespace DataTool.ToolLogic.Extract
{
    public class ExtractMapEnvFlags : ExtractFlags
    {
        [CLIFlag(Default = false, Flag = "skip-map-env-sound", Help = "Skip map enviornment sound extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnviornmentSound;

        [CLIFlag(Default = false, Flag = "skip-map-env-lut", Help = "Skip map enviornment lut extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnviornmentLUT;

        [CLIFlag(Default = false, Flag = "skip-map-env-blend", Help = "Skip map enviornment blend cubemap extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnviornmentBlendCubemap;

        [CLIFlag(Default = false, Flag = "skip-map-env-ground", Help = "Skip map enviornment ground cubemap extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnviornmentGroundCubemap;

        [CLIFlag(Default = false, Flag = "skip-map-env-sky", Help = "Skip map enviornment sky cubemap extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnviornmentSkyCubemap;

        [CLIFlag(Default = false, Flag = "skip-map-env-skybox", Help = "Skip map enviornment skybox extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnviornmentSkybox;

        [CLIFlag(Default = false, Flag = "skip-map-env-lut", Help = "Skip map enviornment entity extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnviornmentEntity;
    }
}
