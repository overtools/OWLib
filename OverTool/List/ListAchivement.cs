using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool.List
{
    public class ListAchievement : IOvertool
    {
        public string Title => "List Achievements";
        public char Opt => '\0';
        public string FullOpt => "list-achievement";
        public string Help => null;
        public uint MinimumArgs => 0;
        public ushort[] Track => new ushort[1] { 0x68 };
        public bool Display => true;

        public static Tuple<string, STUD, IInventorySTUDInstance> GetRewardItem(ulong key, Dictionary<ulong, Record> map, CASCHandler handler) {
            if (!map.ContainsKey(key)) {
                return null;
            }

            STUD stud = new STUD(Util.OpenFile(map[key], handler));
            if (stud.Instances == null) {
                return null;
            }
            if (stud.Instances[0] == null) {
                return null;
            }
            IInventorySTUDInstance instance = stud.Instances[0] as IInventorySTUDInstance;
            if (instance == null) {
                return null;
            }

            return new Tuple<string, STUD, IInventorySTUDInstance>(Util.GetString(instance.Header.name.key, map, handler), stud, instance);
        }

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags)
        {
            Console.Out.WriteLine("Listing Achievements");
            foreach (ulong key in track[0x68])
            {
                if (!map.ContainsKey(key))
                {
                    continue;
                }

                using (Stream input = Util.OpenFile(map[key], handler))
                {
                    if (input == null)
                    {
                        continue;
                    }
                    STUD stud = new STUD(input);
                    if (stud.Instances == null || stud.Instances[0] == null)
                    {
                        continue;
                    }

                    Achievement achieve = stud.Instances[0] as Achievement;
                    if (achieve == null)
                    {
                        continue;
                    }

                    string name = Util.GetString(achieve.Header.name, map, handler);
                    string description = Util.GetString(achieve.Header.description1, map, handler);

                    Tuple<string, STUD, IInventorySTUDInstance> reward = GetRewardItem(achieve.Header.reward, map, handler);
                    string rewardName = reward.Item1;
                    STUD rewardStud = reward.Item2;
                    IInventorySTUDInstance rewardInstance = reward.Item3;

                    Console.Out.WriteLine(name);
                    Console.Out.WriteLine("\t{0} ({1} {2})", rewardName, rewardInstance.Header.rarity, rewardStud.Instances[0].Name);
                    Console.Out.WriteLine("\t{0}", description);
                }
            }
        }
    }
}
