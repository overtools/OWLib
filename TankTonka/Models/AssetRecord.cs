using System.Collections.Generic;
using System.Runtime.Serialization;
using TankLib;

namespace TankTonka.Models {
    public class AssetRecord {
        [DataMember(Name = "guid")]
        public teResourceGUID GUID;
        
        [DataMember(Name = "references")]
        public HashSet<teResourceGUID> References;

        [DataMember(Name = "stu_info")]
        public Common.StructuredDataInfo StructuredDataInfo;
        
        [DataMember(Name = "chunked_info")]
        public Common.ChunkedDataInfo ChunkedDataInfo;
    }
}