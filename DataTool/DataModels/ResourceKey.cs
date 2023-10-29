using TankLib.STU;
using TankLib.STU.Types;

namespace DataTool.DataModels {
    public class ResourceKey {
        public string KeyID { get; set; }
        public string Value { get; set; }

        public ResourceKey(STUResourceKey resourceKey) {
            KeyID = resourceKey.GetKeyIDString();
            Value = resourceKey.GetKeyValueString();
        }
    }
}
