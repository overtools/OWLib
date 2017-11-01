using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.ToolLogic.Extract;
using OWLib;
using OWLib.Writer;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic {
    public class Animation {
        public static void Save(ICLIFlags flags, string path, HashSet<AnimationInfo> animations) {
            bool convertAnims = false;
            if (flags is ExtractFlags extractFlags) {
                convertAnims = extractFlags.ConvertAnimations && !extractFlags.Raw;
                if (extractFlags.SkipAnimations) return;
            }
            SEAnimWriter animWriter = new SEAnimWriter();
            foreach (AnimationInfo modelAnimation in animations) {
                using (Stream animStream = OpenFile(modelAnimation.GUID)) {
                    if (animStream == null) {
                        continue;
                    }
                    
                    OWLib.Animation animation = new OWLib.Animation(animStream);

                    if (convertAnims) {
                        string animOutput = Path.Combine(path,$"{animation.Header.priority}\\{GUID.LongKey(modelAnimation.GUID):X12}{animWriter.Format}");
                        CreateDirectoryFromFile(animOutput);
                        using (Stream fileStream = new FileStream(animOutput, FileMode.Create)) {
                            animWriter.Write(animation, fileStream, new object[] { });
                        }
                    } else {
                        animStream.Position = 0;
                        string animOutput2 = Path.Combine(path, $"{animation.Header.priority}\\{GUID.LongKey(modelAnimation.GUID):X12}.{GUID.Type(modelAnimation.GUID):X3}");
                        CreateDirectoryFromFile(animOutput2);
                        using (Stream fileStream = new FileStream(animOutput2, FileMode.Create)) {
                            animStream.CopyTo(fileStream);
                        }
                    }
                }
            }
        }
    }
}