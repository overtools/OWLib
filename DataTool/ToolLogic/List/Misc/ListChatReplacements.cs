using DataTool.DataModels.Chat;
using DataTool.Flag;
using DataTool.JSON;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-chat-replacements", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListChatReplacements : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(data, flags);
                    return;
                }
        }

        private static ChatReplacementsContainer GetData() {
            foreach (ulong key in TrackedFiles[0x54]) {
                var stu = GetInstance<STU_15A511F9>(key);
                if (stu == null) continue;

                return new ChatReplacementsContainer(stu, key);
            }

            return null;
        }
    }
}