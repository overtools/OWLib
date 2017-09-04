using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CASCLib;
using Newtonsoft.Json;
using OWLib;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool.JSON {
    public class JSONPortrait : IOvertool {
        public string Title => "JSON output portrait levels";
        public char Opt => '\0';
        public string FullOpt => "json-portrait";
        public string Help => "output.json";
        public uint MinimumArgs => 1;
        public ushort[] Track => new ushort[1] { 0xA5 };
        public bool Display => true;

        public struct JSONPkg {
            public uint Tier;
            public ushort LevelModifier;
            public ushort Stars;
            public long MinimumLevel;
            public bool IsMajorPoint;
        }

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            Dictionary<ulong, JSONPkg> dict = new Dictionary<ulong, JSONPkg>();
            foreach (ulong key in track[0xA5]) {
                if (!map.ContainsKey(key)) {
                    continue;
                }

                using (Stream input = Util.OpenFile(map[key], handler)) {
                    if (input == null) {
                        continue;
                    }
                    STUD stud = new STUD(input);
                    if (stud.Instances == null || stud.Instances[0] == null) {
                        continue;
                    }

                    PortraitItem master = stud.Instances[0] as PortraitItem;
                    if (master == null) {
                        continue;
                    }
                    dict[key] = new JSONPkg {
                        LevelModifier = master.Data.bracket,
                        Tier = master.Data.tier,
                        Stars = master.Data.star,
                        MinimumLevel = (master.Data.bracket - 11) * 10 + 1
                    };
                }
            }

            if (Path.GetDirectoryName(flags.Positionals[2]).Trim().Length > 0 && !Directory.Exists(Path.GetDirectoryName(flags.Positionals[2]))) {
                Directory.CreateDirectory(Path.GetDirectoryName(flags.Positionals[2]));
            }
            using (Stream file = File.OpenWrite(flags.Positionals[2])) {
                using (TextWriter writer = new StreamWriter(file)) {
                    writer.Write(JsonConvert.SerializeObject(dict, Formatting.Indented));
                }
            }
        }
    }
}
