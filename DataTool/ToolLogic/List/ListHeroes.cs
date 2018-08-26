using System;
using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;
using TankLib.Helpers;

namespace DataTool.ToolLogic.List {
    [Tool("list-heroes", Description = "List heroes", TrackTypes = new ushort[] {0x75}, CustomFlags = typeof(ListFlags))]
    public class ListHeroes : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            Dictionary<string, Hero> heroes = GetHeroes();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(heroes, flags);
                    return;
                }

            IndentHelper indentLevel = new IndentHelper();
            
            foreach (KeyValuePair<string, Hero> hero in heroes) {
                Log($"{hero.Value.Name}");
                if (hero.Value.Description != null)
                    Log($"{indentLevel + 1}Description: {hero.Value.Description}");
                
                Log($"{indentLevel + 1}Gender: {hero.Value.Gender}");
                
                TankLib.Helpers.Logger.Log24Bit(ConsoleSwatch.ColorReset, null, false, null, $"{indentLevel + 1}Color: {hero.Value.GalleryColor.ToHex()} ");
                TankLib.Helpers.Logger.Log24Bit(hero.Value.GalleryColor.ToForeground(), null, true, null, "██████");

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

        public Dictionary<string, Hero> GetHeroes() {
            Dictionary<string, Hero> @return = new Dictionary<string, Hero>();

            foreach (ulong key in TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(key);
                if (hero == null) continue;

                string name = GetString(hero.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(key):X}";

                @return[name] = new Hero(key, hero);
            }

            return @return;
        }
    }
}