using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.InventoryItem;
using OWLib.Writer;

namespace OverTool.ExtractLogic {
    public class VictoryPose {
        public static void Parse(ulong key, Dictionary<ulong, Record> map, CASCHandler handler, Dictionary<ulong, ulong> animList, ulong parent = 0) {
            if (key == 0) {
                return;
            }
            if (parent == 0) {
                parent = key;
            }
            if (!map.ContainsKey(key)) {
                return;
            }

            STUD record = new STUD(Util.OpenFile(map[key], handler), true, STUDManager.Instance, false, true);
            if (record.Instances == null) {
                return;
            }
            foreach (ISTUDInstance inst in record.Instances) {
                if (inst == null) {
                    continue;
                }
                if (inst.Name == record.Manager.GetName(typeof(VictoryPoseItem))) {
                    VictoryPoseItem item = (VictoryPoseItem)inst;
                    Parse(item.Data.f0BF.key, map, handler, animList, key);
                } else if (inst.Name == record.Manager.GetName(typeof(Pose))) {
                    Pose r = (Pose)inst;
                    foreach (OWRecord animation in new OWRecord[3] { r.Header.animation1, r.Header.animation2, r.Header.animation3 }) {
                        ulong bindingKey = animation.key;
                        if (!map.ContainsKey(bindingKey)) {
                            continue;
                        }
                        animList[bindingKey] = parent;
                        Skin.FindAnimationsSoft(bindingKey, animList, new Dictionary<ulong, ulong>(), new HashSet<ulong>(), map, handler, new HashSet<ulong>(), new Dictionary<ulong, List<ImageLayer>>(), bindingKey);
                    }
                } 
            }
        }

        public static void Parse(ulong keyk, string path, Dictionary<ulong, Record> map, CASCHandler handler) {
            Dictionary<ulong, ulong> animList = new Dictionary<ulong, ulong>();
            Parse(keyk, map, handler, animList, keyk);

            SEAnimWriter animWriter = new SEAnimWriter();
            foreach (KeyValuePair<ulong, ulong> kv in animList) {
                ulong parent = kv.Value;
                ulong key = kv.Key;
                string outpath = string.Format("{0}{2:X12}{1}{3:X12}.{4:X3}", path, Path.DirectorySeparatorChar, GUID.Index(parent), GUID.LongKey(key), GUID.Type(key));
                if (!Directory.Exists(Path.GetDirectoryName(outpath))) {
                    Directory.CreateDirectory(Path.GetDirectoryName(outpath));
                }
                using (Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
                    Stream output = Util.OpenFile(map[key], handler);
                    if (output != null) {
                        output.CopyTo(outp);
                        Console.Out.WriteLine("Wrote raw animation {0}", outpath);
                        output.Close();
                    }
                }
                outpath = string.Format("{0}{2:X12}{1}{3:X12}{4}", path, Path.DirectorySeparatorChar, GUID.Index(parent), GUID.LongKey(key), animWriter.Format);

                using (Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
                    Stream output = Util.OpenFile(map[key], handler);
                    if (output != null) {
                        try {
                            Animation anim = new Animation(output, false);
                            animWriter.Write(anim, outp, new object[] { });
                            Console.Out.WriteLine("Wrote animation {0}", outpath);
                        } catch {
                            Console.Error.WriteLine("Error with animation {0:X12}.{1:X3}", GUID.Index(key), GUID.Type(key));
                        }
                    }
                }
            }
        }

        public static void Extract(ulong key, STUD stud, string output, string heroName, string name, string itemGroup, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, List<char> furtherOpts) {
            string dest = string.Format("{0}{1}{2}{1}{3}{1}{5}{1}{4}{1}", output, Path.DirectorySeparatorChar, Util.Strip(Util.SanitizePath(heroName)), Util.SanitizePath(stud.Instances[0].Name), Util.SanitizePath(name), Util.SanitizePath(itemGroup));
            Parse(key, dest, map, handler);
        }
    }
}
