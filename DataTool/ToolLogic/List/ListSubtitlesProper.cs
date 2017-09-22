using System;
using System.Collections.Generic;
using System.Linq;
using CASCLib;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using Newtonsoft.Json;
using STULib.Types;
using STULib.Types.Generic;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.List {
    [Tool("list-subtitles-real", Description = "List subtitles (from audio data)", TrackTypes = new ushort[] {0x5F}, CustomFlags = typeof(ListFlags))]
    public class ListSubtitlesProper : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class RealSubtitleInfo {
            public string Subtitle;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong SubtitleGUID;
            
            [JsonConverter(typeof(GUIDConverter))]
            public ulong AudioGUID;

            public RealSubtitleInfo(ulong guid, ulong audioGuid, string text) {
                SubtitleGUID = guid;
                AudioGUID = audioGuid;
                Subtitle = text;
            }
        }

        public void Parse(ICLIFlags toolFlags) {
            List<SoundInfo> subtitles = GetSubtitles();
            
            // todo: json

            // if (toolFlags is ListFlags flags)
            //     if (flags.JSON) {
            //         ParseJSON(subtitles, flags);
            //         return;
            //     }

            IndentHelper indentLevel = new IndentHelper();
            foreach (SoundInfo subtitle in subtitles) {
                Log($"{subtitle.GUID.ToString()} - {subtitle.Subtitle}");
            }
        }

        public List<SoundInfo> GetSubtitles() {
            Dictionary<ulong, List<SoundInfo>> sounds = new MultiDictionary<ulong, SoundInfo>();
            List<SoundInfo> subtitleSounds = new List<SoundInfo>();

            foreach (ulong key in TrackedFiles[0x5F]) {
                sounds = Sound.FindSounds(sounds, new Common.STUGUID(key));
                foreach (SoundInfo soundInfo in sounds.SelectMany(c => c.Value).Where(x => x.Subtitle != null).ToList()) {
                    if (!subtitleSounds.Contains(soundInfo)) {
                        subtitleSounds.Add(soundInfo);
                    }
                }
            }

            return subtitleSounds;
        }
    }
}