using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool.ExtractLogic {
    class Portrait {
        public static void Save(ulong key, string path, Dictionary<ulong, Record> map, bool quiet, CASCHandler handler) {
            if (!map.ContainsKey(key)) {
                return;
            }
            if (!Directory.Exists(Path.GetDirectoryName(path))) {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            ulong imageDataKey = (key & 0xFFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL;

            using (Stream outp = File.Open(path, FileMode.Create, FileAccess.Write)) {
                if (map.ContainsKey(imageDataKey)) {
                    Texture tex = new Texture(Util.OpenFile(map[key], handler), Util.OpenFile(map[imageDataKey], handler));
                    tex.Save(outp);
                } else {
                    TextureLinear tex = new TextureLinear(Util.OpenFile(map[key], handler));
                    tex.Save(outp);
                }
            }

            if (!quiet) {
                Console.Out.WriteLine("Wrote portrait {0}", path);
            }
        }

        public static void Extract(STUD itemStud, string output, string heroName, string itemName, string itemGroup, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            string path = string.Format("{0}{1}{2}{1}{3}{1}{5}{1}{4}{1}", output, Path.DirectorySeparatorChar, Util.Strip(Util.SanitizePath(heroName)), Util.SanitizePath(itemStud.Instances[0].Name), Util.SanitizePath(itemName), Util.SanitizePath(itemGroup));

            if (itemStud.Instances == null) {
                return;
            }
            PortraitItem item = (PortraitItem)itemStud.Instances[0];
            if (item == null) {
                return;
            }
            if (!map.ContainsKey(item.Data.portrait.key)) {
                return;
            }
            string level = $"Level {(item.Data.bracket - 11) * 10 + 1}";
            if (!File.Exists($"{path}{level}.dds")) {
                Save(item.Data.portrait.key,  $"{path}{level}.dds", map, quiet, handler);
            }
            string s = "s";
            if (item.Data.star == 1) {
                s = "";
            }
            if (!File.Exists($"{path} {item.Data.star} Star{s}.dds")) {
                Save(item.Data.portrait2.key, $"{path} {item.Data.star} Star{s}.dds", map, quiet, handler);
            }
        }
    }
}
