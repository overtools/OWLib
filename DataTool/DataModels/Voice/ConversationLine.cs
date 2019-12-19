using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels.Voice {
    [DataContract]
    public class ConversationLine {
        [DataMember]
        public teResourceGUID GUID;
        
        [DataMember]
        public teResourceGUID VoicelineGUID;

        [DataMember]
        public ulong Position;

        public ConversationLine(ulong key) {
            var stu = GetInstance<STUVoiceConversationLine>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public ConversationLine(STUVoiceConversationLine stu, ulong key = default) {
            Init(stu, key);
        }

        private void Init(STUVoiceConversationLine convoLine, ulong key = default) {
            GUID = (teResourceGUID) key;
            VoicelineGUID = (teResourceGUID) convoLine.m_lineGUID;
            Position = convoLine.m_B4D405A1;
        }
    }
}
