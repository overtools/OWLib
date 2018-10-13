using DataTool.Flag;
using DataTool.ToolLogic.Extract;

namespace DataTool.ToolLogic.Dbg {
    [Tool("brrap", Description = "I hear da call", IsSensitive = true, CustomFlags = typeof(ExtractFlags))]
    class DebugVoodoo : ITool {
        public void Parse(ICLIFlags toolFlags) {
        }
    }
}
