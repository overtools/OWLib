using System;
using DataTool.Flag;
using TankLib.STU.Types;
using System.Collections.Generic;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-brawl-name", Description = "List brawl names", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListBrawlName : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(data, flags);
                    return;
                }

            Log("Brawl Names:");
            foreach (var item in data) {
                if (item.Name != null)
                    Log(item.Name);
            }
        }

        private static List<BrawlName> GetData() {
            var @return = new List<BrawlName>();

            foreach (var key in TrackedFiles[0xD9]) {
                var stu = GetInstance<STU_4B259FE1>(key);
                if (stu == null) continue;

                @return.Add(new BrawlName {
                    GUID = (teResourceGUID) key,
                    Name = Helper.IO.GetString(stu.m_name)
                });
            }

            return @return;
        }

        public class BrawlName {
            public teResourceGUID GUID;
            public string Name;
        }
    }
}