using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-keys", Description = "List resource keys", IsSensitive = true, CustomFlags = typeof(ListFlags))]
    public class ListResourceKeys : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ListFlags) toolFlags;
            var keys = GetKeys();

            if (flags.JSON) {
                OutputJSON(keys, flags);
                return;
            }

            foreach (KeyValuePair<teResourceGUID, ResourceKey> key in keys) {
                Log($"{key.Key}: {key.Value.KeyID} {key.Value.Value}");
            }
        }

        public Dictionary<teResourceGUID, ResourceKey> GetKeys() {
            var @return = new Dictionary<teResourceGUID, ResourceKey>();

            foreach (teResourceGUID key in TrackedFiles[0x90]) {
                STUResourceKey resourceKey = GetInstance<STUResourceKey>(key);
                if (resourceKey == null) continue;
                @return[key] = new ResourceKey(resourceKey);
            }

            return @return;
        }
    }
}
