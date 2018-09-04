using System.Runtime.Serialization;

namespace DataTool.DataModels {
    /// <summary>
    /// UXDisplayText data model
    /// </summary>
    [DataContract]
    public class UXDisplayText {
        /// <summary>
        /// String value
        /// </summary>
        [DataMember]
        public string Value;

        public UXDisplayText(ulong guid) {
            Value = Helper.IO.GetString(guid);
        }
    }
}
