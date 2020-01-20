using System.Collections.Generic;
using System.Diagnostics;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-ux-strings-or-something", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListUxStringsOrSomething : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(data, flags);
                    return;
                }
        }

        private static List<UxStringContainer> GetData() {
            var @return = new List<UxStringContainer>();

            foreach (ulong key in TrackedFiles[0x114]) {
                var stu = STUHelper.GetInstance<STU_6649A4C0>(key);

                var stringContainer = new UxStringContainer {
                    GUID = (teResourceGUID) key,
                    Strings = new List<UxString>()
                };
                
               foreach (var str in stu.m_81125A2C) {
                   stringContainer.Strings.Add(new UxString {
                       VirtualO1C = str.m_id,
                       DisplayName = IO.GetString(str.m_displayName)
                   });
               }
               
               @return.Add(stringContainer);
            }

            return @return;
        }

        public class UxStringContainer {
            public teResourceGUID GUID;
            public List<UxString> Strings;
        }

        public class UxString {
            public teResourceGUID VirtualO1C;
            public string DisplayName;
        }
    }
}