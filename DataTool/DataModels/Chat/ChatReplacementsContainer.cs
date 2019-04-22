using System.Linq;
using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels.Chat {
    [DataContract]
    public class ChatReplacementsContainer {
        [DataMember]
        public teResourceGUID GUID;

        [DataMember]
        public ChatReplacementSettings[] ReplacementsSettings;

        public ChatReplacementsContainer(ulong key) {
            var stu = GetInstance<STU_15A511F9>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public ChatReplacementsContainer(STU_15A511F9 stu, ulong key = default) {
            Init(stu, key);
        }

        private void Init(STU_15A511F9 cReplacementContainer, ulong key = default) {
            GUID = (teResourceGUID) key;
            ReplacementsSettings = cReplacementContainer.m_97BAD106.Select(x => new ChatReplacementSettings(x)).ToArray();
        }
    }
}