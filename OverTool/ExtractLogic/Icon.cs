using System;
using System.Collections.Generic;
using System.IO;
using CASCLib;
using OWLib;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool.ExtractLogic {
    class Icon {
        public static void Extract(STUD itemStud, string output, string heroName, string itemName, string itemGroup, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            string path = string.Format("{0}{1}{2}{1}{3}{1}{5}{1}{4}.dds", output, Path.DirectorySeparatorChar, Util.Strip(Util.SanitizePath(heroName)), Util.SanitizePath(itemStud.Instances[0].Name), Util.SanitizePath(itemName), Util.SanitizePath(itemGroup));

            if (itemStud.Instances == null) {
                return;
            }
            IconItem item = (IconItem)itemStud.Instances[0];
            if (item == null) {
                return;
            }
            if (!map.ContainsKey(item.Data.decal.key)) {
                return;
            }
            STUD decalStud = new STUD(Util.OpenFile(map[item.Data.decal.key], handler));
            if (decalStud.Instances == null) {
                return;
            }
            Decal decal = (Decal)decalStud.Instances[0];
            if (decal == null) {
                return;
            }
            if (!map.ContainsKey(decal.Records[0].definiton.key)) {
                return;
            }

            ImageDefinition definition = new ImageDefinition(Util.OpenFile(map[decal.Records[0].definiton.key], handler));

            ulong imageKey = definition.Layers[0].Key;
            if (!map.ContainsKey(imageKey)) {
                return;
            }
            if (!Directory.Exists(Path.GetDirectoryName(path))) {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            ulong imageDataKey = (imageKey & 0xFFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL;

            using (Stream outp = File.Open(path, FileMode.Create, FileAccess.Write)) {
                if (map.ContainsKey(imageDataKey)) {
                    Texture tex = new Texture(Util.OpenFile(map[imageKey], handler), Util.OpenFile(map[imageDataKey], handler));
                    tex.Save(outp);
                } else {
                    TextureLinear tex = new TextureLinear(Util.OpenFile(map[imageKey], handler));
                    tex.Save(outp);
                }
            }
            if (!quiet) {
                Console.Out.WriteLine("Wrote icon {0}", path);
            }
        }
    }
}
