using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;
using OWLib.Types;
using OWLib.Types.STUD.Binding;
using OverTool.ExtractLogic;
using OWLib.Writer;

namespace OverTool.List {
    class ExtractLootbox : IOvertool {
        public string Help => "output";
        public uint MinimumArgs => 0;
        public char Opt => 'L';
        public string Title => "Extract Lootboxes";
        public ushort[] Track => new ushort[1] { 0xCF };

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, string[] args) {
            Console.Out.WriteLine();
            foreach (ulong master in track[0xCF]) {
                if (!map.ContainsKey(master)) {
                    continue;
                }
                STUD lootbox = new STUD(Util.OpenFile(map[master], handler));
                Lootbox box = lootbox.Instances[0] as Lootbox;
                if (box == null) {
                    continue;
                }

                Extract(box.Master.model, box, track, map, handler, quiet, args);
                Extract(box.Master.alternate, box, track, map, handler, quiet, args);
            }
        }

        private void Extract(ulong model, Lootbox lootbox, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, string[] args) {
            if (model == 0 || !map.ContainsKey(model)) {
                return;
            }

            string output = $"{args[0]}{Path.DirectorySeparatorChar}{Util.SanitizePath(lootbox.EventName)}{Path.DirectorySeparatorChar}";

            STUD stud = new STUD(Util.OpenFile(map[model], handler));

            HashSet<ulong> models = new HashSet<ulong>();
            Dictionary<ulong, ulong> animList = new Dictionary<ulong, ulong>();
            HashSet<ulong> parsed = new HashSet<ulong>();
            Dictionary<ulong, List<ImageLayer>> layers = new Dictionary<ulong, List<ImageLayer>>();
            Dictionary<ulong, ulong> replace = new Dictionary<ulong, ulong>();

            foreach (ISTUDInstance inst in stud.Instances) {
                if (inst == null) {
                    continue;
                }
                if (inst.Name == stud.Manager.GetName(typeof(ComplexModelRecord))) {
                    ComplexModelRecord r = (ComplexModelRecord)inst;
                    ulong modelKey = r.Data.model.key;
                    models.Add(modelKey);
                    Skin.FindAnimations(r.Data.animationList.key, animList, replace, parsed, map, handler, models, layers, modelKey);
                    Skin.FindAnimations(r.Data.secondaryAnimationList.key, animList, replace, parsed, map, handler, models, layers, modelKey);
                    Skin.FindTextures(r.Data.material.key, layers, replace, parsed, map, handler);
                }
            }

            Skin.Save(null, output, "", "", replace, parsed, models, layers, animList, new List<char>() { }, track, map, handler, model, false, quiet);
        }
    }
}