using System;
using System.Collections.Generic;
using DataTool.Flag;
using TankLib.STU;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-specialhelper", Description = "Extract new enities (debug)", TrackTypes = new ushort[] {0xA5}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugSpecialHelper : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            SpecialHelper(toolFlags);
        }
        
        public void SpecialHelper(ICLIFlags toolFlags) {
            ExtractDebugNewEntities.VersionInfo versionInfo = ExtractDebugNewEntities.GetGUIDVersionInfo(@"D:\ow\resources\verdata\49154.guids");

            const Enum_BABC4175 lootboxType = Enum_BABC4175.SummerGames;

            HashSet<ulong> addedUnlocks = new HashSet<ulong>();
            foreach (var progressionGuid in TrackedFiles[0x58]) {
                STUProgressionUnlocks progressionUnlocks = GetInstance<STUProgressionUnlocks>(progressionGuid);
                
                if (progressionUnlocks?.m_lootBoxesUnlocks == null) continue;
                foreach (STULootBoxUnlocks lootBoxUnlocks in progressionUnlocks.m_lootBoxesUnlocks) {
                    ProcessLootBoxUnlocks(lootBoxUnlocks, versionInfo, lootboxType, addedUnlocks);
                }
            }

            foreach (ulong genericSettingsGuid in TrackedFiles[0x54]) {
                STUGenericSettings_PlayerProgression playerProgression =
                    GetInstance<STUGenericSettings_PlayerProgression>(genericSettingsGuid);
                if (playerProgression == null) continue;
                
                foreach (STULootBoxUnlocks lootBoxUnlocks in playerProgression.m_lootBoxesUnlocks) {
                    ProcessLootBoxUnlocks(lootBoxUnlocks, versionInfo, lootboxType, addedUnlocks);
                }

                break;
            }
            
            Console.Out.WriteLine("public static readonly ulong[] SummerGames2018 = new ulong[] {");
            foreach (ulong addedUnlock in addedUnlocks) {
                Console.Out.WriteLine($"    0x{addedUnlock:X8},");
            }
            Console.Out.WriteLine("};");
        }

        public static void ProcessLootBoxUnlocks(STULootBoxUnlocks lootBoxUnlocks, ExtractDebugNewEntities.VersionInfo versionInfo, Enum_BABC4175 lootboxType, HashSet<ulong> addedUnlocks) {
            if (lootBoxUnlocks?.m_unlocks?.m_unlocks == null) return;
            if (lootBoxUnlocks.m_lootboxType != lootboxType) return;
            foreach (teStructuredDataAssetRef<STUUnlock> unlock in lootBoxUnlocks.m_unlocks.m_unlocks) {
                //Unlock unlockModel = new Unlock(unlock);
                //if (unlockModel.Type != "Skin") continue;
                
                if (!versionInfo.GUIDs.Contains(unlock)) {
                    addedUnlocks.Add(unlock);
                }
            }
        }
    }
}