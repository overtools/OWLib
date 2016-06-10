using System;
using System.Collections.Generic;
using System.IO;
using OWLib;

namespace TextureTool {
  class Recursive {
    private string dDest;
    private string d004;
    private string d04D;

    public Recursive(string destFile, string f004, string f04D) {
      dDest = destFile;
      if(!Directory.Exists(dDest)) {
        Directory.CreateDirectory(dDest);
      }
      d004 = f004;
      d04D = f04D;
      
      string[] f004s = Directory.GetFiles(d004, "*.004");
      foreach(string f004i in f004s) {
        try {
          using(Stream s004 = File.Open(f004i, FileMode.Open, FileAccess.Read)) {
            TextureLinear master = new TextureLinear(s004, true);
            string fn004 = Path.GetFileNameWithoutExtension(f004i);
            Console.Out.WriteLine("Opened Texture {0}. W: {1} H: {2} F: {3}", fn004, master.Header.width, master.Header.height, master.Format.ToString());
            using(Stream sDDS = File.Open(string.Format("{0}{1}{2}.dds", dDest, Path.DirectorySeparatorChar, fn004), FileMode.OpenOrCreate, FileAccess.Write)) {
              if(master.Loaded == false && d04D != null) {
                s004.Position = 0;
                string fn04D = (master.Header.indice - 1).ToString("X").PadLeft(fn004.Length - 8, '0') + fn004.Substring(fn004.Length - 8); // try to find the texture
                string f04Di = string.Format("{0}{1}{2}.04D", d04D, Path.DirectorySeparatorChar, fn04D);
                using(Stream s04D = File.Open(f04Di, FileMode.Open, FileAccess.Read)) {
                  Texture tex = new Texture(s004, s04D);
                  tex.ToDDS(sDDS);
                  Console.Out.WriteLine("Converted texture pair {0}.dds", fn004);
                }
              } else {
                master.Save(sDDS);
                Console.Out.WriteLine("Converted texture {0}.dds", fn004);
              }
            }
          }
        } catch(Exception ex) {
          Console.Error.WriteLine("Failed to convert texture.");
          Console.Error.WriteLine(ex.ToString());
        }
      }
    }
  }
}
