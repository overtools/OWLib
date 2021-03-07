using System.Collections.Generic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-tips", Description = "List game tips", CustomFlags = typeof(ListFlags))]
    public class ListTips : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();

            if (toolFlags is ListFlags flags)
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
                var stu = STUHelper.GetInstance<STU_96ABC153>(key);
                if (stu == null) continue;

                @return[key] = IO.GetString(stu.m_94672A2A);
            }

            return @return;
        }
    }
}
