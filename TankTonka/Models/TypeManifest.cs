using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TankTonka.Models {
    public class TypeManifest {
        [DataMember(Name = "type")]
        public Common.AssetRepoType Type;

        [DataMember(Name = "stu_info")]
        public Common.StructuredDataInfo StructuredDataInfo;
        [DataMember(Name = "chunked_info")]
        public Common.ChunkedDataInfo ChunkedDataInfo;
        
        [DataMember(Name = "guid_reference_types")]
        public HashSet<Common.AssetRepoType> GUIDReferenceTypes;
    }
}