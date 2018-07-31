using System;
using System.Collections.Generic;
using DataTool.Flag;
using DataTool.JSON;
using Newtonsoft.Json;
using TankLib.STU;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-keys", Description = "List resource keys", TrackTypes = new ushort[] {0x90}, CustomFlags = typeof(ListFlags))]
    public class ListResourceKeys : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class KeyInfo {
            public string KeyID;
            public string KeyValue;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong GUID;

            public KeyInfo(ulong dataGUID, string keyID, string keyValue) {
                GUID = dataGUID;
                KeyID = keyID;
                KeyValue = keyValue;
            }
        }

        public void Parse(ICLIFlags toolFlags) {
            Dictionary<string, KeyInfo> keys = GetKeys();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(keys, flags);
                    return;
                }

            foreach (KeyValuePair<string, KeyInfo> key in keys) {
                Log($"{key.Key}: {key.Value.KeyID} {key.Value.KeyValue}");
            }
        }

        public Dictionary<string, KeyInfo> GetKeys() {
            Dictionary<string, KeyInfo> @return = new Dictionary<string, KeyInfo>();

            foreach (ulong key in TrackedFiles[0x90]) {
                STUResourceKey resourceKey = GetInstance<STUResourceKey>(key);
                if (resourceKey == null) continue;
                @return[GetFileName(key)] = new KeyInfo(key, resourceKey.GetKeyIDString(), resourceKey.GetKeyValueString());
            }

            return @return;
        }
    }
}