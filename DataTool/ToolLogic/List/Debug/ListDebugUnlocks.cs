using System;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using TankLib.CASC;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.List.Debug {
    [Tool("list-debug-unlocks", Description = "List unlocks (debug)", TrackTypes = new ushort[] {0x75}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListDebugUnlocks : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetSoundbanks();
        }

        public void GetSoundbanks() {
            foreach (ulong key in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(key);
                
                if (hero == null) continue;
                
                STUHeroUnlocks unlocks = GetInstance<STUHeroUnlocks>(hero.LootboxUnlocks);
                if (unlocks == null) continue;
                
                Console.Out.WriteLine($"{GetString(hero.Name)}");
                
                if (unlocks.LootboxUnlocks != null) {
                    foreach (STULootBoxUnlocks eventUnlocks in unlocks.LootboxUnlocks) {
                        if (eventUnlocks?.Unlocks?.Unlocks == null) continue;

                        string eventKey;
                        if (ItemEvents.GetInstance().EventsNormal.ContainsKey((uint) eventUnlocks.Event)) {
                            eventKey = $"Event/{ItemEvents.GetInstance().EventsNormal[(uint)eventUnlocks.Event]}";
                        } else {
                            eventKey = $"Unknown{eventUnlocks.Event}";
                        }
                        
                        Console.Out.WriteLine($"    {eventKey}:");

                        foreach (ulong unlockKey in eventUnlocks.Unlocks.Unlocks) {
                            STUUnlock unlock = GetInstance<STUUnlock>(unlockKey);

                            if (unlock == null) {
                                try {
                                    using (OpenFileUnsafe(Files[unlockKey], out ulong _)) { }
                                } catch (BLTEKeyException e) {
                                    Console.Out.WriteLine($"Missing key: {e.MissingKey:X}");
                                }
                                continue;
                            }
                            
                            Console.Out.WriteLine($"        {GetString(unlock.CosmeticName)} ({unlock.CosmeticRarity} {unlock.RealName})");
                        }
                    }
                }
                Console.Out.WriteLine();
            }
        }
    }
}