#nullable enable
using System.Linq;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels.Voice;

public class Conversation {
    public teResourceGUID GUID { get; set; }
    public teResourceGUID StimulusGUID { get; set; }
    public float Weight { get; set; }
    public ConversationLine[]? Voicelines { get; set; }

    public Conversation(STUVoiceConversation? stu, ulong key = default) {
        Init(stu, key);
    }

    private void Init(STUVoiceConversation? voiceConvo, ulong key = default) {
        if (voiceConvo == null) return;

        GUID = (teResourceGUID) key;
        StimulusGUID = voiceConvo.m_stimulus;
        Weight = voiceConvo.m_weight;
        Voicelines = voiceConvo.m_90D76F17?.Select(x => new ConversationLine(x)).ToArray();
    }

    public static Conversation? Load(ulong key) {
        var stu = GetInstance<STUVoiceConversation>(key);
        if (stu == null) return null;
        return new Conversation(stu, key);
    }
}