using System.Collections.Generic;
using System.Linq;
using DataTool.Flag;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-profanity-filters", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListProfanityFilters : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(data, flags);
                    return;
                }
        }

        public class ProfanityFilterContainer {
            public teResourceGUID GUID;
            public string[] BadWords = new string[0];
        }

        private static IList<ProfanityFilterContainer> GetData() {
            IList<ProfanityFilterContainer> @return = new List<ProfanityFilterContainer>();
            
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