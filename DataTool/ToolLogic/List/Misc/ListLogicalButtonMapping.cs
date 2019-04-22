using System.Collections.Generic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using TankLib.STU.Types.Enums;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-logical-button-mapping", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListLogicalButtonMapping : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(data, flags);
                    return;
                }
        }

        private static List<LogicalButtonMapping> GetData() {
            var mapping = new List<LogicalButtonMapping>();
            
            foreach (ulong key in TrackedFiles[0x96]) {
                var stu = GetInstance<STU_C5243F93>(key);
                if (stu == null) continue;
                
                mapping.Add(new LogicalButtonMapping {
                    Name = IO.GetString(stu.m_name),
                    Category = stu.m_category,
                    LogicalButton = stu.m_logicalButton,
                    ModeFlags = stu.m_modeFlags,
                    SortValue = stu.m_sortValue,
                    UnkEnum = stu.m_946AA91D
                });
            }

            return mapping;
        }
        
        public class LogicalButtonMapping {
            public string Name;
            public STUInputLogicalButtonCategory Category;
            public STULogicalButton LogicalButton;
            public Enum_0DEE7CD5 ModeFlags;
            public float SortValue;
            public Enum_3D49D804 UnkEnum;
        }
    }
}