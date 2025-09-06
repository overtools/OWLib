using DataTool.Flag;
using System.Collections.Generic;
using DataTool.DataModels.Voice;
using DataTool.JSON;
using static DataTool.Program;

namespace DataTool.ToolLogic.List.Misc;

[Tool("list-conversations", Description = "List conversations", CustomFlags = typeof(ListFlags), IsSensitive = true)]
public class ListConversations : JSONTool, ITool {
    public void Parse(ICLIFlags toolFlags) {
        var flags = (ListFlags) toolFlags;
        var data = GetData();
        OutputJSON(data, flags);
    }

    private static List<Conversation> GetData() {
        var @return = new List<Conversation>();

        foreach (var key in TrackedFiles[0xD0]) {
            var conversation = Conversation.Load(key);
            if (conversation == null) continue;
            @return.Add(conversation);
        }

        return @return;
    }
}