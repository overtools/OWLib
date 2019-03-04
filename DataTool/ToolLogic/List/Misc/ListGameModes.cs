using System.Collections.Generic;
using DataTool.DataModels.GameModes;
using DataTool.Flag;
using DataTool.JSON;
using static DataTool.Program;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-gamemodes", Description = "List game modes", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListGameModes : JSONTool, ITool {
        public List<GameMode> GetGameModes() {
            List<GameMode> gameModes = new List<GameMode>();
            foreach (var key in TrackedFiles[0xC5]) {
                var gamemode = new GameMode(key);
                if (gamemode.GUID == 0) continue;

                gameModes.Add(gamemode);
            }

            return gameModes;
        }

        public void Parse(ICLIFlags toolFlags) {
            List<GameMode> gameModes = GetGameModes();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(gameModes, flags);
                    return;
                }


            foreach (GameMode gameMode in gameModes) {
                if (string.IsNullOrWhiteSpace(gameMode.Name)) continue;
                Log($"{gameMode.Name} ({gameMode.InternalName})");
            }
        }
    }
}