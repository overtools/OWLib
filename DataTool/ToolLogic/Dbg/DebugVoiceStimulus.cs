using System.Linq;
using DataTool.Flag;
using DataTool.JSON;
using DataTool.ToolLogic.Dump;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Dbg {
    [Tool("debug-voice-stimulus", Description = "I hear da call", IsSensitive = true, CustomFlags = typeof(DumpFlags))]
    class DebugVoiceStimulus : JSONTool, ITool {
        class DebugVoiceStim {
            public string Guid;
            public ulong Key;
            public string CategoryGuid;
            public string OtherGuid;
            public STUVoiceStimulus StimulusSet;
        }
        
        public void Parse(ICLIFlags toolFlags) {
            
            var sets = Program.TrackedFiles[075].Select(key => {
                var set = GetInstance<STUVoiceStimulus>(key);
                return new DebugVoiceStim {
                    Guid = teResourceGUID.AsString(key),
                    Key = key,
                    CategoryGuid = teResourceGUID.AsString(set?.m_category),
                    OtherGuid = teResourceGUID.AsString(set?.m_87DCD58E),
                    StimulusSet = set
                };
            });

            OutputJSONAlt(sets, toolFlags as DumpFlags);
        }
    }
}
