using System;
using System.Collections.Generic;
using DataTool.Flag;
using TankLib.STU;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-specialhelper", Description = "generate special categories", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugSpecialHelper : ITool {
        public void Parse(ICLIFlags toolFlags) {
            SpecialHelper(toolFlags);
        }
        
        public void SpecialHelper(ICLIFlags toolFlags) {
            var guids = ExtractDebugNewEntities.GetGUIDs(@"D:\ow\resources\verdata\50951.guids");

            const Enum_BABC4175 lootboxType = Enum_BABC4175.Halloween;

            HashSet<ulong> addedUnlocks = new HashSet<ulong>();
            foreach (var progressionGuid in TrackedFiles[0x58]) {
                STUProgressionUnlocks progressionUnlocks = GetInstance<STUProgressionUnlocks>(progressionGuid);
                
                if (progressionUnlocks?.m_lootBoxesUnlocks == null) continue;
                foreach (STULootBoxUnlocks lootBoxUnlocks in progressionUnlocks.m_lootBoxesUnlocks) {
                    ProcessLootBoxUnlocks(lootBoxUnlocks, guids, lootboxType, addedUnlocks);
                }
            }

            foreach (ulong genericSettingsGuid in TrackedFiles[0x54]) {
                STUGenericSettings_PlayerProgression playerProgression =
                    GetInstance<STUGenericSettings_PlayerProgression>(genericSettingsGuid);
                if (playerProgression == null) continue;
                
                foreach (STULootBoxUnlocks lootBoxUnlocks in playerProgression.m_lootBoxesUnlocks) {
                    ProcessLootBoxUnlocks(lootBoxUnlocks, guids, lootboxType, addedUnlocks);
                }

                break;
            }
            
            Console.Out.WriteLine("new ulong[] {");
            foreach (ulong addedUnlock in addedUnlocks) {
                Console.Out.WriteLine($"    0x{addedUnlock:X8},");
            }
            Console.Out.WriteLine("};");
        }

        public static void ProcessLootBoxUnlocks(STULootBoxUnlocks lootBoxUnlocks, HashSet<ulong> guids, Enum_BABC4175 lootboxType, HashSet<ulong> addedUnlocks) {
            if (lootBoxUnlocks?.m_unlocks?.m_unlocks == null) return;
            if (lootBoxUnlocks.m_lootboxType != lootboxType) return;
            foreach (teStructuredDataAssetRef<STUUnlock> unlock in lootBoxUnlocks.m_unlocks.m_unlocks) {
                //Unlock unlockModel = new Unlock(unlock);
                //if (unlockModel.Type != "Skin") continue;
                
                if (!guids.Contains(unlock)) {
                    addedUnlocks.Add(unlock);
                }
            }
        }
    }
}