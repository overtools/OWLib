using System;
using System.Collections.Generic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using Newtonsoft.Json;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.List {
    [Tool("list-subtitles", Description = "List subtitles", TrackTypes = new ushort[] {0x71}, CustomFlags = typeof(ListFlags))]
    public class ListSubtitles : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class SubtitleInfo {
            public string[] Subtitles;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong GUID;

            public SubtitleInfo(ulong guid, string[] text) {
                GUID = guid;
                Subtitles = text;
            }
        }

        public void Parse(ICLIFlags toolFlags) {
            Dictionary<string, SubtitleInfo> subtitles = GetSubtitles();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(subtitles, flags);
                    return;
                }

            IndentHelper indentLevel = new IndentHelper();
            foreach (KeyValuePair<string, SubtitleInfo> subtitle in subtitles) {
                Log($"{subtitle.Key}");
                foreach (string s in subtitle.Value.Subtitles) {
                    Log($"{indentLevel+1}{s}");
                }
                Log();
            }
        }

        public void GetSubtitle(List<string> output, STU_A94C5E3B subtitle) {
            if (subtitle == null) return;
            output.Add(subtitle.m_text);
        }

        public string[] GetSubtitlesInternal(STU_7A68A730 subtileContainer) {
            List<string> @return = new List<string>();

            GetSubtitle(@return, subtileContainer.m_798027DE);
            GetSubtitle(@return, subtileContainer.m_A84AA2B5);
            GetSubtitle(@return, subtileContainer.m_D872E45C);
            GetSubtitle(@return, subtileContainer.m_1485B834);
            return @return.ToArray();
        }

        public Dictionary<string, SubtitleInfo> GetSubtitles() {
            Dictionary<string, SubtitleInfo> @return = new Dictionary<string, SubtitleInfo>();

            foreach (ulong key in TrackedFiles[0x71]) {
                STU_7A68A730 subtitleContainer = GetInstance<STU_7A68A730>(key);
                if (subtitleContainer == null) continue;

                @return[GetFileName(key)] = new SubtitleInfo(key, GetSubtitlesInternal(subtitleContainer));
            }

            return @return;
        }
    }
}