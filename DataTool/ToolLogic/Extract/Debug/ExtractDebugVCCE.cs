/*using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using DataTool.Flag;
using static DataTool.Program;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-vcce", Description = "Extract VCCE as png (debug)", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugVCCE : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ExtractVCCE(toolFlags);
        }

        public void ExtractVCCE(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            const string container = "DebugVCCE";
            
            foreach (ulong key in TrackedFiles[0x8F]) {
                // if (GUID.Index(key) != 0xDEADBEEF) continue;

                using (Stream chunkedStream = OpenFile(key)) {
                    Chunked chunked = new Chunked(chunkedStream);

                    int vcceIndex = 0;

                    VCCE[] vcces = chunked.GetAllOfTypeFlat<VCCE>();
                    foreach (VCCE vcce in vcces) {
                        using (Bitmap b = new Bitmap(vcce.Data.TableCount, 1)) {
                            for (int i = 0; i < vcce.SecondaryEntries.Length; i++) {
                                VCCE.SecondaryEntry secondary = vcce.SecondaryEntries[i];
                                VCCE.Entry primary = vcce.Entries[i];
                                byte col = (byte) (primary.A * 255);
                                Color color = Color.FromArgb(col, col, col);
                                b.SetPixel(i, 0, color);
                            }
                            string file = Path.Combine(basePath, container, GetFileName(key), $"{vcceIndex}.png");
                            CreateDirectoryFromFile(file);
                            b.Save(file, ImageFormat.Png);
                        }
                        vcceIndex++;
                    }
                }
            }
        }
    }
}*/