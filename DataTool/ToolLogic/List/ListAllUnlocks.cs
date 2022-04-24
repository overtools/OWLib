using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;

namespace DataTool.ToolLogic.List {
    [Tool("list-all-unlocks", Description = "List all unlocks", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListAllUnlocks : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            if (toolFlags is ListFlags flags) {
                OutputJSON(GetData(), flags);
            }
        }

        public static Dictionary<teResourceGUID, UnlockAll> GetData() {
            var allUnlocks = new Dictionary<teResourceGUID, UnlockAll>();

            foreach (var key in TrackedFiles[0xA5]) {
                var guid = (teResourceGUID) key;
                if (!allUnlocks.ContainsKey(guid)) {
                    var unlock = new UnlockAll(key);
                    if (unlock.GUID != 0) {
                        allUnlocks[guid] = unlock;
                    }
                }
            }

            var heroes = TrackedFiles[0x75].Select(STUHelper.GetInstance<STUHero>);

            foreach (var stuHero in heroes) {
                var hero = new Hero(stuHero);
                var progression = new ProgressionUnlocks(stuHero);
                foreach (var unlock in progression.IterateUnlocks()) {
                    if (allUnlocks.ContainsKey(unlock.GUID)) {
                        allUnlocks[unlock.GUID].Hero = hero.Name;
                    }
                }
            }

            return allUnlocks;
        }
    }

    public class UnlockAll : Unlock {
        public string Hero;

        public UnlockAll(ulong guid) : base(guid) { }
    }
}