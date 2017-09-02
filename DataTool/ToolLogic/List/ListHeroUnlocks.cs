using System;
using DataTool.Flag;

namespace DataTool.ToolLogic.List {
    [Tool("list-unlocks", Description = "List hero unlocks", TrackTypes = new ushort[] { 0x75 })]
    public class ListHeroUnlocks : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            throw new NotImplementedException();
        }
    }
}
