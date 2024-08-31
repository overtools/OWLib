using System.Collections.Generic;
using DataTool.DataModels.GameModes;
using DataTool.Flag;
using DataTool.JSON;
using static DataTool.Program;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-gamemodes", Description = "List game modes", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListGameModes : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ListFlags) toolFlags;
            var gameModes = GetGameModes();

            if (flags.JSON) {
                OutputJSON(gameModes, flags);
                return;
            }

            foreach (GameMode gameMode in gameModes) {
                if (string.IsNullOrWhiteSpace(gameMode.Name)) continue;
                Log($"{gameMode.Name}");
            }
        }

        public List<GameMode> GetGameModes() {
            var gameModes = new List<GameMode>();
            foreach (var key in TrackedFiles[0xC5]) {
                var gamemode = new GameMode(key);
                if (gamemode.GUID == 0) continue;

                gameModes.Add(gamemode);
            }

            return gameModes;
        }
    }
}
