using DataTool.Flag;

namespace DataTool {
    public interface ITool {
        void Parse(ICLIFlags toolFlags);
    }
}