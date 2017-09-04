using System.Collections.Generic;
using System.IO;
using CASCLib;
using OWLib;
using OWLib.Types;

namespace OverTool.ExtractLogic {
    public class ItemAnimation {
        public static void Extract(ulong key, STUD stud, string output, string heroName, string name, string itemGroup, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            string dest = string.Format("{0}{1}{2}{1}{3}{1}{5}{1}{4}{1}", output, Path.DirectorySeparatorChar, Util.Strip(Util.SanitizePath(heroName)), Util.SanitizePath(stud.Instances[0].Name), Util.SanitizePath(name), Util.SanitizePath(itemGroup));

            Dictionary<ulong, ulong> animList = new Dictionary<ulong, ulong>();
            HashSet<ulong> models = new HashSet<ulong>();
            Dictionary<ulong, List<ImageLayer>> layers = new Dictionary<ulong, List<ImageLayer>>();
            Dictionary<ulong, List<ulong>> sound = new Dictionary<ulong, List<ulong>>();
            Skin.FindAnimations(key, sound, animList, new Dictionary<ulong, ulong>(), new HashSet<ulong>(), map, handler, models, layers, key);
            if (animList.Count > 0) {
                if (!Directory.Exists(dest)) {
                    Directory.CreateDirectory(dest);
                }
                Skin.Save(null, dest, heroName, name, new Dictionary<ulong, ulong>(), new HashSet<ulong>(), models, layers, animList, flags, track, map, handler, 0, true, quiet, sound, 0);
            }
        }
    }
}
