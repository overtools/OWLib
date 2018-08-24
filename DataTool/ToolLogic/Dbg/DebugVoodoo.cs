using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataTool.Flag;
using TankLib;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Debug
{
    [Tool("brrap", Description = "I hear da call", TrackTypes = new ushort[] { 0x1B }, IsSensitive = true)]

    class DebugVoodoo : ITool
    {
        public void IntegrateView(object sender)
        {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags)
        {
            var indices = new List<ulong>
            {
                0x0000000017C7,
                0x0000000017C8,
                0x0000000017F8,
                0x0000000017FA,
                0x00000000180B
            };
            foreach(var guid in Program.TrackedFiles[0x1B])
            {
                if(indices.Contains(teResourceGUID.LongKey(guid)))
                {
                    var stu = OpenSTUSafe(guid);
                    System.Diagnostics.Debugger.Break();
                }
            }
        }
    }
}
