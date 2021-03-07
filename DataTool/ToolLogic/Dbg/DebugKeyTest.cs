using DataTool.Flag;
using TACTLib.Core;
using TACTLib.Core.Product.Tank;

namespace DataTool.ToolLogic.Dbg {
    [Tool("te-key-test", Description = "", IsSensitive = true, CustomFlags = typeof(ToolFlags))]
    class DebugKeyTest : ITool {
        public void Parse(ICLIFlags toolFlags) {
            foreach (var guid in Program.TankHandler.m_assets.Keys) {
                var stream = Program.TankHandler.OpenFile(guid);
                var blte = stream as BLTEStream ?? (stream as GuidStream)?.BaseStream as BLTEStream;
                if (blte != null) {
                    if (blte.Keys.Contains("6FDE3253A9819DD8")) { }
                }
            }
        }
    }
}
