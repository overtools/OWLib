using DataTool.Flag;

namespace DataTool.ToolLogic.Render {
    [Tool("render-ui-elements", Description = "Render UI elements", TrackTypes = new ushort[] { 0x5E }, CustomFlags = typeof(RenderFlags), IsSensitive = true)]
    public class RenderUIElements : ITool {
        public void IntegrateView(object sender) {
            throw new System.NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            
        }
    }
}
