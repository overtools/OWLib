using DataTool.Flag;

namespace DataTool {
    public interface ITool {
        void IntegrateView(object sender);

        void Parse(ICLIFlags toolFlags);
    }
}