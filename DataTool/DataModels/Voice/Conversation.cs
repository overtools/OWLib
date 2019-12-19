using System.Linq;
using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels.Voice {
    [DataContract]
    public class Conversation {
        [DataMember]
        public teResourceGUID GUID;
        
        [DataMember]
        public teResourceGUID StimulusGUID;

        [DataMember]
        public float Weight;

        [DataMember]
        public ConversationLine[] Voicelines;

        public Conversation(ulong key) {
            var stu = GetInstance<STUVoiceConversation>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public Conversation(STUVoiceConversation stu, ulong key = default) {
            Init(stu, key);
        }

        private void Init(STUVoiceConversation voiceConvo, ulong key = default) {
            GUID = (teResourceGUID) key;
            StimulusGUID = voiceConvo.m_stimulus;
            Weight = voiceConvo.m_weight;
            Voicelines = voiceConvo.m_voiceConversationLine?.Select(x => new ConversationLine(x)).ToArray();
        }
    }
}
