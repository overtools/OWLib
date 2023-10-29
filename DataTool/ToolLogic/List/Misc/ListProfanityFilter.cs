using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.Flag;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-profanity-filters", Description = "What did you say you ******* *****", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListProfanityFilters : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ListFlags) toolFlags;
            var data = GetData();
            OutputJSON(data, flags);
        }

        public class ProfanityFilterContainer {
            public teResourceGUID GUID;
            public string[] BadWords = Array.Empty<string>();
        }

        private static IList<ProfanityFilterContainer> GetData() {
            var @return = new List<ProfanityFilterContainer>();

            foreach (var key in TrackedFiles[0x7F]) {
                var stu = GetInstance<STU_E55DA1F4>(key);
                if (stu == null) continue;

                @return.Add(new ProfanityFilterContainer {
                    GUID = (teResourceGUID) key,
                    BadWords = stu.m_F627FDCA?.Select(x => x.Value).ToArray()
                });
            }

            return @return;
        }
    }
}
