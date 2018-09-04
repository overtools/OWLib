using System.Runtime.Serialization;
using TankLib.STU;
using TankLib.STU.Types;

namespace DataTool.DataModels {
    [DataContract]
    public class ResourceKey {
        [DataMember]
        public string KeyID;
        
        [DataMember]
        public string Value;

        public ResourceKey(STUResourceKey resourceKey) {
            KeyID = resourceKey.GetKeyIDString();
            Value = resourceKey.GetKeyValueString();
        }
    }
}
