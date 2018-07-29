using System;
using System.IO;
using DataTool.DataModels;
using DataTool.Flag;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-general", Description = "Extract general unlocks", TrackTypes = new ushort[] {0x54}, CustomFlags = typeof(ExtractFlags))]
    public class ExtractGeneral : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetGeneralUnlocks(toolFlags);
        }

        public void GetGeneralUnlocks(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            string path = Path.Combine(basePath, "General");

            foreach (var key in TrackedFiles[0x54]) {
                STUGenericSettings_PlayerProgression progression = GetInstanceNew<STUGenericSettings_PlayerProgression>(key);
                if (progression == null) continue;

                if (progression.m_lootBoxesUnlocks != null) {
                    foreach (STULootBoxUnlocks lootBoxUnlocks in progression.m_lootBoxesUnlocks) {
                        if (lootBoxUnlocks.m_unlocks == null) continue;
                        
                        string boxName = ExtractHeroUnlocks.GetLootBoxName((uint)lootBoxUnlocks.m_lootboxType);
                
                        Unlock[] unlocks = Unlock.GetArray(lootBoxUnlocks.m_unlocks);
                
                        ExtractHeroUnlocks.SaveUnlocks(flags, unlocks, path, boxName, null, null, null, null);
                    }
                }
                if (progression.m_additionalUnlocks != null) {
                    foreach (STUAdditionalUnlocks additionalUnlocks in progression.m_additionalUnlocks) {
                        if (additionalUnlocks == null) continue;
                        Unlock[] unlocks = Unlock.GetArray(additionalUnlocks.m_unlocks);
                        
                        ExtractHeroUnlocks.SaveUnlocks(flags, unlocks, path, "Standard", null, null, null, null);
                    }
                }
                if (progression.m_otherUnlocks != null) {
                    Unlock[] unlocks = Unlock.GetArray(progression.m_otherUnlocks);
                    
                    ExtractHeroUnlocks.SaveUnlocks(flags, unlocks, path, "Achievement", null, null, null, null);
                }
            }
        }
    }
}