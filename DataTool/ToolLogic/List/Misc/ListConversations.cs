﻿using DataTool.Flag;
using TankLib.STU.Types;
using System.Collections.Generic;
using DataTool.DataModels.Voice;
using DataTool.JSON;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List.Misc {
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
                var stu = GetInstance<STUVoiceConversation>(key);
                if (stu == null) continue;

                @return.Add(new Conversation(stu, key));
            }

            return @return;
        }
    }
}
