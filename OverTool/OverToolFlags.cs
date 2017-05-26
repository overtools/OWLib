using OverTool.Flags;

namespace OverTool {
    public class OverToolFlags : ICLIFlags {
        // Standard

        [CLIFlag(Flag = "directory", Positional = 0, Help = "Overwatch Directory", Required = true)]
        public string OverwatchDirectory;
        
        [CLIFlag(Flag = "mode", Positional = 1, Help = "Extraction Mode", Required = true)]
        public string Mode;

        [CLIFlag(Default = "enUS", Flag = "language", Help = "Language to load", NeedsValue = true, Valid = new string[] { "deDE", "enUS", "esES", "esMX", "frFR", "itIT", "jaJP", "koKR", "plPL", "ptBR", "ruRU", "zhCN", "zhTW" })]
        [Alias(Alias = "L")]
        [Alias(Alias = "lang")]
        public string Language;
        
        [CLIFlag(Default = false, Flag = "quiet", Help = "Suppress majority of output messages", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBoolean" })]
        [Alias(Alias = "q")]
        [Alias(Alias = "silent")]
        public bool Quiet;

        [CLIFlag(Default = false, Flag = "skip-keys", Help = "Skip key detection", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBoolean" })]
        [Alias(Alias = "n")]
        public bool SkipKeys;

        [CLIFlag(Default = false, Flag = "expert", Help = "Output more asset information", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBoolean" })]
        [Alias(Alias = "ex")]
        public bool Expert;

        // Mode Dependent
        [CLIFlag(Default = 'w', Flag = "model-format", NeedsValue = true, Help = "Target type to convert models to (+ to disable)", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagChar" })]
        [Alias(Alias = "f")]
        public char ModelFormat;

        [CLIFlag(Default = 'S', Flag = "animation-format", NeedsValue = true, Help = "Target type to convert animations to (+ to disable)", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagChar" })]
        [Alias(Alias = "a")]
        public char AnimFormat;

        [CLIFlag(Default = false, Flag = "raw", Help = "Skip conversion entirely", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBoolean" })]
        [Alias(Alias = "r")]
        public bool Raw;

        [CLIFlag(Default = true, Flag = "raw-model", Help = "Extract raw model with converted model", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBooleanInv" })]
        [Alias(Alias = "rw")]
        public bool RawModel;

        [CLIFlag(Default = true, Flag = "raw-animation", Help = "Extract raw animation with converted animation", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBooleanInv" })]
        [Alias(Alias = "ra")]
        public bool RawAnimation;

        [CLIFlag(Default = false, Flag = "no-texture", Help = "Skip texture extraction", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBoolean" })]
        [Alias(Alias = "T")]
        [Alias(Alias = "skip-texture")]
        [Alias(Alias = "skip-tex")]
        public bool SkipTextures;

        [CLIFlag(Default = false, Flag = "no-animation", Help = "Skip animation extraction", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBoolean" })]
        [Alias(Alias = "A")]
        [Alias(Alias = "skip-animation")]
        [Alias(Alias = "skip-anim")]
        public bool SkipAnimations;

        [CLIFlag(Default = false, Flag = "no-model", Help = "Skip model extraction", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBoolean" })]
        [Alias(Alias = "M")]
        [Alias(Alias = "skip-model")]
        public bool SkipModels;

        [CLIFlag(Default = false, Flag = "no-sound", Help = "Skip sound extraction", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBoolean" })]
        [Alias(Alias = "S")]
        [Alias(Alias = "skip-sound")]
        public bool SkipSound;

        [CLIFlag(Default = false, Flag = "collision", Help = "Export collision models", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBoolean" })]
        [Alias(Alias = "C")]
        public bool ExportCollision;

        [CLIFlag(Default = false, Flag = "no-ref", Help = "Skip refpose extraction", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBoolean" })]
        [Alias(Alias = "R")]
        [Alias(Alias = "skip-ref")]
        public bool SkipRefpose;

        [CLIFlag(Default = false, Flag = "no-gui", Help = "Skip GUI icon extraction", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagBoolean" })]
        [Alias(Alias = "I")]
        [Alias(Alias = "skip-gui")]
        public bool SkipGUI;

        [CLIFlag(Default = -1, Flag = "weaponskin", Help = "Weaponskin to use", Parser = new string[] { "OverTool.Flags.CLIFlagAttribute", "CLIFlagInt" })]
        [Alias(Alias = "W")]
        public int WeaponSkinIndex;

    }
}
