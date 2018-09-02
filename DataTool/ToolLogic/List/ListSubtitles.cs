using System;
using System.Collections.Generic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-subtitles", Description = "List subtitles", TrackTypes = new ushort[] {0x71}, CustomFlags = typeof(ListFlags))]
    public class ListSubtitles : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            Dictionary<teResourceGUID, string[]> subtitles = GetSubtitles();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(subtitles, flags);
                    return;
                }

            IndentHelper i = new IndentHelper();
            foreach (KeyValuePair<teResourceGUID, string[]> subtitle in subtitles) {
                Log($"{subtitle.Key}");
                foreach (string s in subtitle.Value) {
                    Log($"{i+1}{s}");
                }
                Log();
            }
        }

        public void GetSubtitle(List<string> output, STU_A94C5E3B subtitle) {
            if (subtitle?.m_text == null) return;
            output.Add(subtitle.m_text);
        }

        public string[] GetSubtitlesInternal(STU_7A68A730 subtitleContainer) {
            List<string> @return = new List<string>();

            GetSubtitle(@return, subtitleContainer.m_798027DE);
            GetSubtitle(@return, subtitleContainer.m_A84AA2B5);
            GetSubtitle(@return, subtitleContainer.m_D872E45C);
            GetSubtitle(@return, subtitleContainer.m_1485B834);
            
            return @return.ToArray();
        }

        public Dictionary<teResourceGUID, string[]> GetSubtitles() {
            Dictionary<teResourceGUID, string[]> @return = new Dictionary<teResourceGUID, string[]>();

            foreach (teResourceGUID key in TrackedFiles[0x71]) {
                STU_7A68A730 subtitleContainer = GetInstance<STU_7A68A730>(key);
                if (subtitleContainer == null) continue;

                @return[key] = GetSubtitlesInternal(subtitleContainer);
            }

            return @return;
        }
    }
}