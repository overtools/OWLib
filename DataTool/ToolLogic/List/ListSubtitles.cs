using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using Newtonsoft.Json;
using OWLib;
using STULib.Types;
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

        public void GetSubtitle(List<string> output, STUSubtitle subtitle) {
            if (subtitle == null) return;
            output.Add(subtitle.Text);
        }

        public string[] GetSubtitlesInternal(STUSubtitleContainer subtileContainer) {
            List<string> @return = new List<string>();

            GetSubtitle(@return, subtileContainer.Subtitle1);
            GetSubtitle(@return, subtileContainer.Subtitle2);
            GetSubtitle(@return, subtileContainer.Subtitle3);
            GetSubtitle(@return, subtileContainer.Subtitle4);
            return @return.ToArray();
        }

        public Dictionary<string, SubtitleInfo> GetSubtitles() {
            Dictionary<string, SubtitleInfo> @return = new Dictionary<string, SubtitleInfo>();

            foreach (ulong key in TrackedFiles[0x71].OrderBy(GUID.Index)) {
                STUSubtitleContainer subtitleContainer = GetInstance<STUSubtitleContainer>(key);
                if (subtitleContainer == null) continue;

                @return[GetFileName(key)] = new SubtitleInfo(key, GetSubtitlesInternal(subtitleContainer));
            }

            return @return;
        }
    }
}