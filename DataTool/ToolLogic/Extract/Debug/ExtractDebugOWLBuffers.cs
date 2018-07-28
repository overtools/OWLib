using System;
using System.IO;
using DataTool.Flag;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;
using static DataTool.Program;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-owlbuffers", Description = "Extract OWL buffers (debug)", TrackTypes = new ushort[] {0xB3}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugOWLBuffers : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetSoundbanks(toolFlags);
        }

        public void GetSoundbanks(ICLIFlags toolFlags) {
            const string container = "OWLBuffers";
            
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            foreach (ulong teamColorGuid in TrackedFiles[0xEC]) {
                STU_73AE9738 teamDef = GetInstanceNew<STU_73AE9738>(teamColorGuid);
                string name = $"{GetString(teamDef.m_4BA3B3CE)} {GetString(teamDef.m_137210AF)}";
                
                STUTeamColor teamColor = GetInstanceNew<STUTeamColor>(teamDef.m_teamColor);
                using (Stream stream = OpenFile(teamColor.m_materialData)) {
                    teMaterialData materialData = new teMaterialData(stream);
                }
            }
        }
    }
}