using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.Flag;
using DataTool.JSON;
using Newtonsoft.Json;
using OWLib;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-strings", Description = "Dumps all the strings", TrackTypes = new ushort[] { 0x7C }, CustomFlags = typeof(DumpFlags))]
    public class DumpStrings : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class StringInfo {
            public string FileName;
            public string String;

            [JsonConverter(typeof(GUIDConverter))]
            [JsonIgnore]
            public ulong GUID;

            public StringInfo(ulong guid, string fileName, string str) {
                GUID = guid;
                String = str;
                FileName = fileName;
            }
        }

        public void Parse(ICLIFlags toolFlags) {
            var strings = GetStrings();

            if (toolFlags is DumpFlags flags) {
                if (flags.JSON) {
                    ParseJSON(strings, flags);
                    return;
                }
            }

            foreach (var str in strings) {
                Log($"{str.FileName}: {str.String}");
            }
        }

        public List<StringInfo> GetStrings() {
            var strings = new List<StringInfo>();

            foreach (var key in TrackedFiles[0x7C].OrderBy(GUID.Index)) {
                if (key == 0xDE0000000006415) {
                    continue;
                    // @OVERWATCH DEVELOPERS
                    // @OVERWATCH DEVELOPERS
                    // @OVERWATCH DEVELOPERS
                    // @OVERWATCH DEVELOPERS
                    
                    // PLEASE CHECK THIS STRING
                    // THANKS
                    
                    // YOU WILL KNOW WHAT I MEAN
                }
                
                var str = GetString(key);
                var fileName = GetFileName(key);
                if (str == null || fileName == null) continue;
                strings.Add(new StringInfo(key, fileName, str));
            }

            return strings;
        }
    }
}