using System;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib.Helpers;

namespace DataTool.ToolLogic.List {
    [Tool("list-heroes", Description = "List heroes", CustomFlags = typeof(ListFlags))]
    public class ListHeroes : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var heroes = Helpers.GetHeroes();
            var flags = (ListFlags) toolFlags;

            if (flags.JSON) {
                OutputJSON(heroes, flags);
                return;
            }

            var indent = new IndentHelper();
            foreach (var (heroGuid, hero) in heroes) {
                Log($"{hero.Name}");
                if (!flags.Simplify) {
                    if (hero.Description != null)
                        Log($"{indent + 1}Description: {hero.Description}");

                    Log($"{indent + 1}Gender: {hero.Gender}");

                    Log($"{indent + 1}Size: {hero.Size}");

                    TankLib.Helpers.Logger.Log24Bit(ConsoleColor.White.AsDOSColor().AsXTermColor().ToForeground(), null, false, Console.Out, null, $"{indent + 1}Color: {hero.GalleryColor.ToHex()} ");
                    TankLib.Helpers.Logger.Log24Bit(hero.GalleryColor.ToForeground(), null, true, Console.Out, null, "██████");

                    TankLib.Helpers.Logger.Log24Bit(ConsoleColor.White.AsDOSColor().AsXTermColor().ToForeground(), null, false, Console.Out, null, $"{indent + 1}sRGB Color: {hero.GalleryColor.ToNonLinear().ToHex()} ");
                    TankLib.Helpers.Logger.Log24Bit(hero.GalleryColor.ToNonLinear().ToForeground(), null, true, Console.Out, null, "██████");

                    if (hero.Loadouts != null) {
                        Log($"{indent + 1}Loadouts:");
                        foreach (var loadout in hero.Loadouts) {
                            Log($"{indent + 2}{loadout.Name}: {loadout.Category}");
                            Log($"{indent + 3}{loadout.Description}");
                        }
                    }

                    if (hero.Perks != null) {
                        Log($"{indent + 1}Perks:");
                        foreach (var loadout in hero.Perks) {
                            Log($"{indent + 2}{loadout.Name}: {loadout.Category}");
                            Log($"{indent + 3}{loadout.Description}");
                        }
                    }

                    Log();
                }
            }
        }
    }
}
