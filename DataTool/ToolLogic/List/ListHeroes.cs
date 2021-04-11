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
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using System.Linq;

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
                            if (loadout.Category == TankLib.STU.Types.Enums.LoadoutCategory.Weapon) {
                                //Weapon stats dump.
                                //000000001683.01B is Torbjorn gun with 18 ammo
                                foreach (var weap_component in GetInstance<STUEntityDefinition>(hero.Value.STU.m_gameplayEntity.GUID)?.m_componentMap) {
                                    foreach (var weapon in (weap_component.Value as STUWeaponComponent)?.m_weapons ?? Enumerable.Empty<dynamic>()) {
                                        if (weapon.m_script != null) {
                                            foreach (dynamic weap_script_node in GetInstance<STUStatescriptGraph>(weapon?.m_script.GUID)?.m_nodes) {
                                                foreach (var member in cont_m_names) {
                                                    var expr_field = GetDynamicFieldVal(GetDynamicFieldVal(weap_script_node, member), "m_expression");
                                                    if (expr_field?.m_D99EF254 != null && opcodes.ContainsKey(expr_field.m_opcodes)) {
                                                        Log($"{indentLevel + 2}{opcodes[expr_field.m_opcodes]}: {String.Join("\n", expr_field.m_D99EF254)}");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            Log($"{indentLevel + 3}{loadout.Description}");
                        }
                    }

                    Log();
                }
            }
        }

        private static Dictionary<byte[], string> opcodes = new Dictionary<byte[], string>(new ByteArrayComparer()) {
                    { new byte[] { 6, 0, 14, 0, 25, 68, 0 },"Weapon ammo " }, //20 is used in weapons with infinite ammo
                    //{ new byte[] { 8, 0, 6, 0, 24, 0 },"Weapon delay between shots " }, //not sure
                    //{ new byte[] { 14, 0, 7, 0, 15, 4, 6, 6, 0, 0, 4, 6, 1, 0},"Weapon max splash damage " }, //kinda inconsistent
                    { new byte[] { 14, 0, 4, 6, 14, 1, 0, 4, 6, 0, 0 } ,"Weapon damage "},
                    { new byte[] { 6,0,0 } ,"Weapon minimal damage "},
                    //{ new byte[] { 8, 0, 6, 0,25,0 } ,"Weapon shots per second "},
                    //{ new byte[] { 6, 0, 14, 0,25,14,1,25,0 } ,"Melee weapon damage "} //TODO melees are not referenced in hero/weapon scripts....... 
                };

        String[] cont_m_names = { "m_value", "m_timeout", "m_amount" }; //container with variables reside in members with such names

        //Dictionaries doesn't work properly with byte[] by default. Custom comparer must be implemented...
        //borrowed from https://stackoverflow.com/questions/1440392/use-byte-as-key-in-dictionary/30353296
        private class ByteArrayComparer : IEqualityComparer<byte[]> {
            public bool Equals(byte[] left, byte[] right) {
                if (left == null || right == null) {
                    return left == right;
                }
                if (left.Length != right.Length) {
                    return false;
                }
                for (int i = 0; i < left.Length; i++) {
                    if (left[i] != right[i]) {
                        return false;
                    }
                }
                return true;
            }
            public int GetHashCode(byte[] key) {
                if (key == null)
                    throw new ArgumentNullException("key");
                int sum = 0;
                foreach (byte cur in key) {
                    sum += cur;
                }
                return sum;
            }
        }
        private static object GetDynamicFieldVal(object obj, string field) {

            if (obj == null)
                return null;
        
           return obj.GetType()?.GetField(field)?.GetValue(obj);
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
