using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using DataTool.ToolLogic.Extract;
using TACTLib;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Dump;

[Tool("dump-lootbox-pools", Description = "Dump Loot Box Pools", CustomFlags = typeof(ExtractFlags))]
public class DumpLootboxPools : JSONTool, ITool {
    private string OutputPath;
    private readonly Dictionary<teResourceGUID, Hero> UnlockToHero = [];
    private readonly Dictionary<teResourceGUID, Unlock> Unlocks = [];

    public void Parse(ICLIFlags toolFlags) {
        var flags = (ExtractFlags)toolFlags;
        if (flags.OutputPath == null)
            throw new Exception("no output path");

        OutputPath = Path.Combine(flags.OutputPath, "LootboxPools");
        CreateDirectorySafe(OutputPath);

        InitHeroes();

        foreach (teResourceGUID lootBoxGUID in TrackedFiles[0x17B]) {
            var lootBox = STUHelper.GetInstance<STU_784F63C5>(lootBoxGUID);
            if (lootBox == null) continue;

            // sourced from GUIDNames.csv
            var lootBoxName = GetGUIDName(lootBoxGUID);

            // todo: which field is correct to use
            // current assets have (x, _, y, _, y, _, y, _)
            //                    (using this one)    ^
            var completePool = LoadPool(lootBox.m_BF7BA867);
            WritePool($"{lootBoxName}_All", completePool);

            var atLeastOnePool = LoadPool(lootBox.m_AAECB0F9);
            WritePool($"{lootBoxName}_AtLeastOne", atLeastOnePool);
        }
    }

    private void InitHeroes() {
        var heroes = Helpers.GetHeroes();

        foreach (var hero in heroes.Values) {
            var progressionUnlocks = new ProgressionUnlocks(hero.STU);
            foreach (var unlock in progressionUnlocks.IterateUnlocks()) {
                Unlocks[unlock.GUID] = unlock;
                UnlockToHero[unlock.GUID] = hero;
            }
        }
    }

    private List<PoolUnlock> LoadPool(teResourceGUID guid) {
        var pool = STUHelper.GetInstance<STU_D9D81E7B>(guid);
        if (pool == null) {
            Logger.Warn(nameof(DumpLootboxPools), $"unable to load pool {guid}");
            return null;
        }

        var poolUnlocks = new List<PoolUnlock>();
        foreach (var rarity in pool.m_5F2BF9E9) {
            foreach (var unlockGUID in rarity.m_unlocks) {
                var unlock = GetUnlock(unlockGUID);
                if (unlock == null) continue; // sanity

                UnlockToHero.TryGetValue(unlock.GUID, out var owningHero);

                poolUnlocks.Add(new PoolUnlock {
                    HeroName = owningHero?.Name,
                    HeroGUID = owningHero?.GUID ?? default,

                    UnlockName = unlock.Name,
                    UnlockGUID = unlock.GUID,

                    Rarity = unlock.Rarity.ToString(),
                    Type = unlock.Type.ToString()
                });
            }
        }

        return poolUnlocks;
    }

    private void WritePool(string name, List<PoolUnlock> unlocks) {
        if (unlocks == null) return;

        using var streamWriter = new StreamWriter(Path.Combine(OutputPath, $"{name}.csv"));
        using var csv = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
        csv.WriteHeader<PoolUnlock>();
        csv.NextRecord();
        foreach (var unlock in unlocks.OrderBy(x => x.HeroName).ThenBy(x => x.UnlockName)) {
            var csvUnlock = unlock;
            if (csvUnlock.HeroName == null) {
                csvUnlock = csvUnlock with { HeroName = "None" };
            }

            csv.WriteRecord(csvUnlock);
            csv.NextRecord();
        }

        OutputJSON(unlocks, Path.Combine(OutputPath, $"{name}.json"));
    }

    private Unlock GetUnlock(teResourceGUID unlockGUID) {
        if (!Unlocks.TryGetValue(unlockGUID, out var unlock)) {
            unlock = Unlock.Load(unlockGUID);
            Unlocks.Add(unlockGUID, unlock);
        }

        return unlock;
    }

    private record PoolUnlock {
        public string HeroName { get; set; }
        public teResourceGUID HeroGUID { get; set; }

        public string UnlockName { get; set; }
        public teResourceGUID UnlockGUID { get; set; }

        public string Rarity { get; set; }
        public string Type { get; set; }
    }
}