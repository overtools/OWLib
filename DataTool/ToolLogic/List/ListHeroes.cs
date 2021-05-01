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
                    bool shown = false;
                    if (hero.Value.Loadouts != null) {
                        Log($"{indentLevel + 1}Loadouts:");
                        foreach (var loadout in hero.Value.Loadouts) {
                            Log($"{indentLevel + 2}{loadout.Name}: {loadout.Category}");
                            if (loadout.Category == TankLib.STU.Types.Enums.LoadoutCategory.Weapon && !shown) {
                                // Weapon stats dump.
                                // 000000001683.01B is Torbjorn gun with 18 ammo
                                shown = true;
                                foreach (var weapon in GetInstance<STUWeaponComponent>(hero.Value.STU.m_gameplayEntity.GUID).m_weapons) {
                                    if (weapon.m_script != null) {
                                        List<STUConfigVarExpression> weap_vars = new List<STUConfigVarExpression>(GetInstances<STUConfigVarExpression>(weapon.m_script.GUID));
                                        if (false) { //debug stuff, will be removed later
                                            foreach (var z in weap_vars) {
                                                if (z.m_expression.m_D99EF254 != null) {
                                                    Log(String.Join(" ", z.m_expression.m_D99EF254));
                                                    Log(String.Join(" ", z.m_expression.m_opcodes));
                                                    Log("\n");
                                                }
                                            }
                                        }

                                        //melee stats (TODO)
                                        var spec_containters = GetInstances<STUStatescriptActionPlayScript>(weapon.m_script.GUID);
                                        if (spec_containters.Length != 0) {
                                            foreach (var script_ref in spec_containters) {
                                                var insts = GetInstances<STUConfigVarExpression>((script_ref.m_script as STU_105E1BCC).m_graph);
                                                weap_vars.AddRange(insts);
                                            }
                                        }

                                        //volley params
                                        var projectile_params = GetInstance<STUStatescriptStateWeaponVolley>(weapon.m_script.GUID);
                                        if (projectile_params != null) {
                                            PrintSTUVar($"{indentLevel + 2}Number of projectiles per shot", projectile_params.m_numProjectilesPerShot, weapon.m_script.GUID);
                                            PrintSTUVar($"{indentLevel + 2}Number of shots per second", projectile_params.m_numShotsPerSecond, weapon.m_script.GUID);
                                            PrintSTUVar($"{indentLevel + 2}Speed of projectile (m/s)", projectile_params.m_projectileSpeed, weapon.m_script.GUID);
                                            PrintSTUVar($"{indentLevel + 2}Damage of projectile (direct hit)", projectile_params?.m_D2D11CE9?.m_modifyHealth.m_amount, weapon.m_script.GUID);
                                            //PrintSTUVar($"{indentLevel + 2}Projectile knockback radius (max)", projectile_params?.m_D2D11CE9?.m_modifyHealth.m_knockbackRings[0].m_radius, weapon.m_script.GUID);
                                        }

                                        //generic stats
                                        foreach (var weap_var in weap_vars) {
                                            if (weap_var.m_expression.m_D99EF254 != null && opcodes.ContainsKey(weap_var.m_expression.m_opcodes)) {
                                                Log($"{indentLevel + 2}{opcodes[weap_var.m_expression.m_opcodes]}: {String.Join("\n", weap_var.m_expression.m_D99EF254)}");
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
                    { new byte[] { 6, 0, 14, 0, 25, 68, 0 },"Weapon ammo " },
                    //{ new byte[] { 8, 0, 6, 0, 24, 0 },"Weapon delay between shots " }, //not sure
                    //{ new byte[] { 14, 0, 7, 0, 15, 4, 6, 6, 0, 0, 4, 6, 1, 0},"Weapon max splash damage " }, //kinda inconsistent
                    { new byte[] { 14, 0, 4, 6, 14, 1, 0, 4, 6, 0, 0 } ,"Weapon damage "},
                    //{ new byte[] { 6,0,0 } ,"Weapon minimal damage "},
                    //{ new byte[] { 8, 0, 6, 0,25,0 } ,"Weapon shots per second "},
                    { new byte[] { 6, 0, 14, 0,25,14,1,25,0 } ,"Melee weapon damage "} // STUStatescriptActionPlayScript has scripts with melee weapon damage!
                };

        private object GetSTUDynValue(STUConfigVar v, ulong stu) {
            if (stu == 0) return null;
            foreach (var i in GetInstances<STUStatescriptActionSetVar>(stu)) {
                if ((v as STUConfigVarDynamic).m_identifier == (i.m_out_Var as STUConfigVarDynamic).m_identifier)
                    return i.m_value.GetType()?.GetField("m_value")?.GetValue(i.m_value);
            }
            return null;
        }

        private object GetSTUVarValue(STUConfigVar v, ulong stu, bool ignoreWeaponDefault = false) {
            if (v.GetType().IsAssignableFrom(typeof(STUConfigVarDynamic))){
                return GetSTUDynValue(v, stu);
            }
            else if (v.GetType().IsSubclassOf(typeof(STUConfigVarNumeric))) {
                var num = v.GetType().GetField("m_value")?.GetValue(v);
                return ignoreWeaponDefault ? (Convert.ToUInt16(num) == 999 ? null : num) : num;
            }
            else if (v.GetType() == (typeof(STU_919FD47C))) {
                var expr = (v as STU_919FD47C)?.m_expression;
                if (expr.m_D99EF254 != null)
                    return string.Join("/", Array.ConvertAll<float, string>(expr.m_D99EF254, o => o.ToString()));
                else if (expr.m_dynamicVars != null) {
                    List<object> vals = new List<object>();
                    foreach (var z in expr.m_dynamicVars) {
                        vals.Add(GetSTUVarValue(z, stu));
                    }
                    return string.Join("/", vals.ConvertAll(Convert.ToString));
                }
            }
            Log($"Unk STUConfigVar type: {v.GetType().FullName}");
            //Log(v.GetType().FullName);
            return null;
        }

        private void PrintSTUVar(string desc, STUConfigVar v, ulong stu = 0) {
            if (v != null && desc != null) {
                var val = GetSTUVarValue(v, stu, true);
                if (val != null) {
                    Log($"{desc}: {val}");
                }
            }
        }

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
