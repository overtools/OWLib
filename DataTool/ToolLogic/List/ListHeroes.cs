using System;
using System.Collections.Generic;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using TankLib.Helpers;

namespace DataTool.ToolLogic.List {
    [Tool("list-heroes", Description = "List heroes", CustomFlags = typeof(ListFlags))]
    public class ListHeroes : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            Dictionary<teResourceGUID, Hero> heroes = GetHeroes();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(heroes, flags);
                    return;
                }

            IndentHelper indentLevel = new IndentHelper();

            foreach (KeyValuePair<teResourceGUID, Hero> hero in heroes) {
                Log($"{hero.Value.Name}");
                if (!(toolFlags as ListFlags).Simplify) {
                    if (hero.Value.Description != null)
                        Log($"{indentLevel + 1}Description: {hero.Value.Description}");

                    Log($"{indentLevel + 1}Gender: {hero.Value.Gender}");

                    Log($"{indentLevel + 1}Size: {hero.Value.Size}");

                    TankLib.Helpers.Logger.Log24Bit(ConsoleColor.White.AsDOSColor().AsXTermColor().ToForeground(), null, false, Console.Out, null, $"{indentLevel + 1}Color: {hero.Value.GalleryColor.ToHex()} ");
                    TankLib.Helpers.Logger.Log24Bit(hero.Value.GalleryColor.ToForeground(), null, true, Console.Out, null, "██████");


                    TankLib.Helpers.Logger.Log24Bit(ConsoleColor.White.AsDOSColor().AsXTermColor().ToForeground(), null, false, Console.Out, null, $"{indentLevel + 1}sRGB Color: {hero.Value.GalleryColor.ToNonLinear().ToHex()} ");
                    TankLib.Helpers.Logger.Log24Bit(hero.Value.GalleryColor.ToNonLinear().ToForeground(), null, true, Console.Out, null, "██████");

                    if (hero.Value.Loadouts != null) {
                        Log($"{indentLevel + 1}Loadouts:");
                        foreach (var loadout in hero.Value.Loadouts) {
                            Log($"{indentLevel + 2}{loadout.Name}: {loadout.Category}");
                            Log($"{indentLevel + 3}{loadout.Description}");
                        }
                    }

                    Log();
                }
            }
        }

        public Dictionary<teResourceGUID, Hero> GetHeroes() {
            var heroes = new Dictionary<teResourceGUID, Hero>();

            foreach (teResourceGUID key in TrackedFiles[0x75]) {
                var hero = new Hero(key);
                if (hero.GUID == 0)
                    continue;

                heroes[key] = hero;
            }

            return heroes;
        }
    }
}
