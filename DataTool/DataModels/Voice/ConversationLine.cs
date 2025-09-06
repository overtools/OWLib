using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels.Voice;

public class ConversationLine {
    public teResourceGUID GUID { get; set; }
    public teResourceGUID VoicelineGUID { get; set; }
    public ulong Position { get; set; }

    public ConversationLine(ConversationLine line) {
        GUID = line.GUID;
        VoicelineGUID = line.VoicelineGUID;
        Position = line.Position;
    }

    public ConversationLine(STUVoiceConversationLine? stu, ulong key = default) {
        Init(stu, key);
    }

    private void Init(STUVoiceConversationLine? convoLine, ulong key = default) {
        if (convoLine == null) return;
        GUID = (teResourceGUID) key;
        VoicelineGUID = (teResourceGUID) convoLine.m_E295B99C;
        Position = convoLine.m_B4D405A1;
    }

    public static ConversationLine? Load(ulong key) {
        var stu = GetInstance<STUVoiceConversationLine>(key);
        if (stu == null) return null;
        return new ConversationLine(stu, key);
    }
}