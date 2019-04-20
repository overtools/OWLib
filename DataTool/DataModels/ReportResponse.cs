using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    [DataContract]
    public class ReportResponse {
        [DataMember]
        public teResourceGUID GUID;

        [DataMember]
        public string Title;

        [DataMember]
        public string Description;

        public ReportResponse(ulong key) {
            var stu = GetInstance<STU_71C0D73D>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public ReportResponse(STU_71C0D73D stu) {
            Init(stu);
        }

        private void Init(STU_71C0D73D reportResponse, ulong key = default) {
            GUID = (teResourceGUID) key;
            Title = GetString(reportResponse.m_BEBA3AEF);
            Description = GetString(reportResponse.m_C65AA24E);
        }
    }
}