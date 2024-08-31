using System.Collections.Generic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-tips", Description = "List game tips", IsSensitive = true, CustomFlags = typeof(ListFlags))]
    public class ListTips : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ListFlags) toolFlags;
            var data = GetData();

            if (flags.JSON) {
                OutputJSON(data, flags);
                return;
            }

            var i = new IndentHelper();
            foreach (var item in data) {
                Log($"{item.Key}");
                Log($"{i + 1}{item.Value}");
            }
        }

        private Dictionary<teResourceGUID, string> GetData() {
            var @return = new Dictionary<teResourceGUID, string>();

            foreach (teResourceGUID key in TrackedFiles[0xD5]) {
                var stu = STUHelper.GetInstance<STU_7AC5B87B>(key);
                if (stu == null) continue;

                @return[key] = IO.GetString(stu.m_6E7E23A2);
            }

            return @return;
        }
    }
}
