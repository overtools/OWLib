using System;
using System.Collections.Generic;
using System.IO;
using CASCLib;
using STULib;
using System.Linq;
using STULib.Types;

namespace OverTool.List {
    public class ListAbilityInfo : IOvertool {
        public string Title => "List Ability info";
        public char Opt => '\0';
        public string FullOpt => "list-ability_info";
        public string Help => null;
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0x9E };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (ulong key in track[0x9E]) {
                if (!map.ContainsKey(key)) {
                    continue;
                }

                using (Stream input = Util.OpenFile(map[key], handler)) {
                    if (input == null) {
                        continue;
                    }
                    ISTU stu = ISTU.NewInstance(input, uint.MaxValue);
                    if (stu.Instances == null) {
                        continue;
                    }

                    STULoadout ability = stu.Instances.First() as STULoadout;
                    if (ability == null) {
                        continue;
                    }

                    Console.Out.WriteLine(Util.GetString(ability.Name, map, handler));
                    if (ability.Category == STULib.Types.Enums.LoadoutCategory.Weapon) {
                        Console.Out.WriteLine($"\t{ability.Category}: {ability.WeaponIndex}");
                    } else {
                        Console.Out.WriteLine($"\t{ability.Category}");
                    }
                    Console.Out.WriteLine($"\t{Util.GetString(ability.Description, map, handler)}");

                    // Console.Out.WriteLine($"{Util.GetString(achieve.Name, map, handler)}, {achieve.AbilityType}, {achieve.WeaponIndex}, {achieve.Unknown3}");
                }
            }
        }
    }
}
