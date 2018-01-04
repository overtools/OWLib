using System;
using System.IO;
using OWLib;

namespace TextureTool {
    class Recursive {
        private string dDest;
        private string d004;
        private string d04D;

        public Recursive(string destFile, string f004, string f04D) {
            dDest = destFile;
            d004 = f004;
            d04D = f04D;

            string[] f004s = Directory.GetFiles(d004, "*.004");
            foreach (string f004i in f004s) {
                try {
                    using (Stream s004 = File.Open(f004i, FileMode.Open, FileAccess.Read)) {
                        TextureLinear master = new TextureLinear(s004, true);
                        string fn004 = Path.GetFileNameWithoutExtension(f004i);
                        Console.Out.WriteLine("Opened Texture {0}. W: {1} H: {2} F: {3} M: {4} S: {5} T: {6}", fn004, master.Header.width, master.Header.height, master.Format.ToString(), master.Header.mips, master.Header.surfaces, master.Header.type);
                        string ntype = "004";
                        if (master.Loaded == false && d04D == null) {
                            Console.Error.WriteLine("Missing 04D texture");
                            continue;
                        }
                        if (master.Header.IsCubemap()) {
                            ntype = "cube";
                        } else if (master.Header.surfaces > 1) {
                            ntype = "multisurface";
                        } else if (master.Loaded == false) {
                            ntype = "04D";
                        }
                        string nindex = fn004.Substring(fn004.Length - 12, 4);
                        // {dDest}\\{ntype}\\{nindex}\\{fn004}.004
                        string nDDS = string.Format("{0}{1}{3}{1}{4}{1}{2}.dds", dDest, Path.DirectorySeparatorChar, fn004, ntype, nindex);
                        string nDDSd = Path.GetDirectoryName(nDDS);
                        if (!Directory.Exists(nDDSd)) {
                            Directory.CreateDirectory(nDDSd);
                        }
                        using (Stream sDDS = File.Open(nDDS, FileMode.Create, FileAccess.Write)) {
                            if (master.Loaded == false) {
                                string fn04D = (master.Header.indice - 1).ToString("X").PadLeft(fn004.Length - 8, '0') + fn004.Substring(fn004.Length - 8); // try to find the texture
                                string f04Di = $"{d04D}{Path.DirectorySeparatorChar}{fn04D}.04D";
                                if (d04D == null || !File.Exists(f04Di)) {
                                    Console.Error.WriteLine("Corresponding 04D {1} file for 004 {0} does not exist", fn004, fn04D);
                                    continue;
                                }
                                s004.Position = 0;
                                using (Stream s04D = File.Open(f04Di, FileMode.Open, FileAccess.Read)) {
                                    Texture tex = new Texture(s004, s04D);
                                    Console.Out.WriteLine("Opened Texture Data {0}. M: {1} S: {2}", fn004, tex.RawHeader.mips, tex.RawHeader.surfaces);
                                    tex.Save(sDDS);
                                    Console.Out.WriteLine("Converted texture pair {0}.dds", fn004);
                                }
                            } else {
                                master.Save(sDDS);
                                Console.Out.WriteLine("Converted texture {0}.dds", fn004);
                            }
                        }
                    }
                } catch (Exception ex) {
                    Console.Error.WriteLine("Failed to convert texture.");
                    Console.Error.WriteLine(ex.ToString());
                }
            }
        }
    }
}
