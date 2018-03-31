using DataTool.Flag;

namespace DataTool.ToolLogic.Extract
{
    public class ExtractMapEnvFlags : ExtractFlags
    {
        [CLIFlag(Default = false, Flag = "skip-map-env-sound", Help = "Skip map Environment sound extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentSound;

        [CLIFlag(Default = false, Flag = "skip-map-env-lut", Help = "Skip map Environment lut extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentLUT;

        [CLIFlag(Default = false, Flag = "skip-map-env-blend", Help = "Skip map Environment blend cubemap extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentBlendCubemap;

        [CLIFlag(Default = false, Flag = "skip-map-env-ground", Help = "Skip map Environment ground cubemap extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentGroundCubemap;

        [CLIFlag(Default = false, Flag = "skip-map-env-sky", Help = "Skip map Environment sky cubemap extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentSkyCubemap;

        [CLIFlag(Default = false, Flag = "skip-map-env-skybox", Help = "Skip map Environment skybox extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentSkybox;

        [CLIFlag(Default = false, Flag = "skip-map-env-lut", Help = "Skip map Environment entity extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentEntity;
    }
}
