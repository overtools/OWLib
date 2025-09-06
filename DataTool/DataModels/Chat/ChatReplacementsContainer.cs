using System.Linq;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels.Chat;

public class ChatReplacementsContainer {
    public teResourceGUID GUID { get; set; }
    public ChatReplacementSettings[] ReplacementsSettings { get; set; }

    public ChatReplacementsContainer(ulong key) {
        var stu = GetInstance<STU_15A511F9>(key);
        Init(stu, key);
    }

    public ChatReplacementsContainer(STU_15A511F9 stu, ulong key = default) {
        Init(stu, key);
    }

    private void Init(STU_15A511F9 cReplacementContainer, ulong key = default) {
        if (cReplacementContainer == null) return;

        GUID = (teResourceGUID) key;
        ReplacementsSettings = cReplacementContainer.m_97BAD106.Select(x => new ChatReplacementSettings(x)).ToArray();
    }
}