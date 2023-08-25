using DataTool.Flag;

namespace TankView {
    public class ToolFlags : ILocaleFlags {
        [CLIFlag(Default = false, Flag = "online", Help = "Allow downloading of corrupted files", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool Online;

        public override bool Validate() => true;
    }
}
