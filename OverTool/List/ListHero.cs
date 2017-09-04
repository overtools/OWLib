using System;
using System.Collections.Generic;
using System.IO;
using CASCLib;
using OWLib;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;
using STULib;
using System.Linq;
using STULib.Types;

namespace OverTool.List {
    public class ListHero : IOvertool {
        public string Title => "List Heroes";
        public char Opt => '\0';
        public string FullOpt => "list-hero";
        public string Help => null;
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0x75 };
        public bool Display => true;

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            foreach (ulong key in track[0x75]) {
                if (!map.ContainsKey(key)) {
                    continue;
                }

                using (Stream input = Util.OpenFile(map[key], handler)) {
                    //if (input == null) {
                    //    continue;
                    //}
                    //ISTU stu = ISTU.NewInstance(input, uint.MaxValue);
                    //if (stu.Instances == null) {
                    //    continue;
                    //}

                    //STUHero hero = stu.Instances.First() as STUHero;
                    //if (hero == null) {
                    //    continue;
                    //}
                    //if (hero.Name != null) {
                    //    Console.Out.WriteLine($"\n{Util.GetString(hero.Name, map, handler)}");
                    //} else {
                    //    Console.Out.WriteLine($"\nNULL:");
                    //}
                    //OWLib.Util.DumpSTUFields(hero);

                    //if (hero.Name != null) { 
                    //    Console.Out.WriteLine($"{Util.GetString(hero.Name, map, handler)}: {hero.Unknown12} {hero.Unknown16} {hero.Unknown24} {hero.Unknown25}");
                    //} else {
                    //    Console.Out.WriteLine($"NULL: {hero.Unknown12} {hero.Unknown16} {hero.Unknown24} {hero.Unknown25}");
                    //}
                    // Console.Out.WriteLine($"\t{hero.PlayerType}\n\t{hero.AvailableAtLaunch}");

                    //Console.Out.WriteLine(Util.GetString(achieve.Name, map, handler));
                    //if (achieve.AbilityType == AbilityType.WEAPON) {
                    //    Console.Out.WriteLine($"\t{achieve.AbilityType}: {achieve.WeaponIndex}");
                    //} else {
                    //    Console.Out.WriteLine($"\t{achieve.AbilityType}");
                    //}
                    //Console.Out.WriteLine($"\t{Util.GetString(achieve.Description, map, handler)}");

                    // Console.Out.WriteLine($"{Util.GetString(achieve.Name, map, handler)}, {achieve.AbilityType}, {achieve.WeaponIndex}, {achieve.Unknown3}");
                }
            }
        }
    }
}
