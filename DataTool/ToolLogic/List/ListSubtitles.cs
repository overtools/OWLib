using System.Collections.Generic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;

namespace DataTool.ToolLogic.List {
    [Tool("list-subtitles", Description = "List subtitles", CustomFlags = typeof(ListFlags))]
    public class ListSubtitles : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ListFlags) toolFlags;
            var subtitles = GetSubtitles();

            if (flags.JSON) {
                OutputJSON(subtitles, flags);
                return;
            }

            IndentHelper i = new IndentHelper();
            foreach (KeyValuePair<teResourceGUID, string[]> subtitle in subtitles) {
                Log($"{subtitle.Key}");
                foreach (var str in subtitle.Value) {
                    Log($"{i + 1}{str}");
                }
            }
        }

        public Dictionary<teResourceGUID, string[]> GetSubtitles() {
            var @return = new Dictionary<teResourceGUID, string[]>();

            foreach (teResourceGUID key in TrackedFiles[0x71]) {
                var subtitle = IO.GetSubtitle(key);
                if (subtitle == null) continue;

                @return[key] = subtitle.m_strings.ToArray();
            }

            return @return;
        }
    }
}
