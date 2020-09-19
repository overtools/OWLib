using System.Collections.Generic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.List {
    [Tool("list-subtitles", Description = "List subtitles", CustomFlags = typeof(ListFlags))]
    public class ListSubtitles : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            Dictionary<teResourceGUID, string> subtitles = GetSubtitles();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(subtitles, flags);
                    return;
                }

            IndentHelper i = new IndentHelper();
            foreach (KeyValuePair<teResourceGUID, string> subtitle in subtitles) {
                Log($"{subtitle.Key}");
                Log($"{i+1}{subtitle.Value}");
            }
        }

        public Dictionary<teResourceGUID, string> GetSubtitles() {
            Dictionary<teResourceGUID, string> @return = new Dictionary<teResourceGUID, string>();

            foreach (teResourceGUID key in TrackedFiles[0x71]) {
                var subtitle = IO.GetString(key);
                if (subtitle == null) continue;

                @return[key] = subtitle;
            }

            return @return;
        }
    }
}