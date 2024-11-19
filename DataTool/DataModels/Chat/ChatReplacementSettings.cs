using System.Collections.Generic;
using System.Linq;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.DataModels.Chat {
    public class ChatReplacementSettings {
        public teResourceGUID VirtualOC3 { get; set; }
        public IEnumerable<string> Triggers { get; set; }
        public IEnumerable<string> GlobalReplacements { get; set; }
        public IEnumerable<string> HeroReplacements { get; set; }
        public IEnumerable<string> Heroes { get; set; }
        public IEnumerable<ChatReplacementReplacementOverrides> ReplacementOverrides { get; set; }

        public ChatReplacementSettings(STU_34F6B4CF chatReplacement) {
            VirtualOC3 = chatReplacement.m_115DDDBF ?? null;

            GlobalReplacements = chatReplacement.m_A7DC8A2F?.Select(x => GetString(x));
            HeroReplacements = chatReplacement.m_B0199D5E?.Select(x => GetString(x));
            Heroes = chatReplacement.m_E9443298?.Select(x => new DataModels.Hero.Hero(x).Name);

            ReplacementOverrides = chatReplacement.m_C6A72790?.Select(x => new ChatReplacementReplacementOverrides(x));

            if (chatReplacement.m_123179A6 != null) {
                var triggersStu = GetInstance<STU_E55DA1F4>(chatReplacement.m_123179A6);
                Triggers = triggersStu?.m_F627FDCA?.Select(x => x.Value);
            }
        }
    }

    public class ChatReplacementReplacementOverrides {
        public string Section { get; set; }
        public IEnumerable<ChatReplacementReplacementOverride> Overrides { get; set; }

        public ChatReplacementReplacementOverrides(STU_8A8E2D47 overrideGroup) {
            Section = overrideGroup.m_7EE81235.Value;
            Overrides = overrideGroup.m_5967BF73?.Select(x => new ChatReplacementReplacementOverride(x));
        }
    }

    public class ChatReplacementReplacementOverride {
        public Enum_0089A8AE[] Locales { get; set; }
        public byte? UnkByte { get; set; }
        public IEnumerable<string> Something { get; set; }

        public ChatReplacementReplacementOverride(STU_7772912A overrideSettings) {
            Locales = overrideSettings.m_B861351F;

            if (overrideSettings is STU_AAE257E9 punctuationOverride) {
                UnkByte = punctuationOverride.m_1AA03F6C;
                Something = punctuationOverride.m_A7DC8A2F?.Select(x => GetString(x));
            }
        }
    }
}
