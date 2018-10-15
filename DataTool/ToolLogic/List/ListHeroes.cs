using System;
using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;
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
                if (hero.Value.Description != null)
                    Log($"{indentLevel + 1}Description: {hero.Value.Description}");
                
                Log($"{indentLevel + 1}Gender: {hero.Value.Gender}");
                
                Log($"{indentLevel + 1}Size: {hero.Value.Size}");
                
                TankLib.Helpers.Logger.Log24Bit(ConsoleSwatch.ColorReset, null, false, Console.Out, null, $"{indentLevel + 1}Color: {hero.Value.GalleryColor.ToHex()} ");
                TankLib.Helpers.Logger.Log24Bit(hero.Value.GalleryColor.ToForeground(), null, true, Console.Out, null, "██████");

                if (hero.Value.Loadouts != null) {
                    Log($"{indentLevel + 1}Loadouts:");
                    foreach (Loadout loadout in hero.Value.Loadouts) {
                        Log($"{indentLevel + 2}{loadout.Name}: {loadout.Category}");
                        Log($"{indentLevel + 3}{loadout.Description}");
                    }
                }

                Log();
            }
        }

        public Dictionary<teResourceGUID, Hero> GetHeroes() {
            Dictionary<teResourceGUID, Hero> @return = new Dictionary<teResourceGUID, Hero>();

            foreach (teResourceGUID key in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(key);
                if (hero == null) continue;

                @return[key] = new Hero(hero);
            }

            return @return;
        }
    }
}