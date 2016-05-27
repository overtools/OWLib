using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib;

namespace TextureTool {
  class Program {
    static void Main(string[] args) {
      if(args.Length < 3) {
        Console.Out.WriteLine("Usage: TextureTool.exe 004_file 04D_file output_file");
        return;
      }

      string headerFile = args[0];
      string dataFile = args[1];
      string destFile = args[2];

      using(Stream headerStream = File.Open(headerFile, FileMode.Open, FileAccess.Read))
      using(Stream dataStream = File.Open(dataFile, FileMode.Open, FileAccess.Read)) {
        Texture tex = new Texture(headerStream, dataStream);
        using(Stream ddsStream = File.Open(destFile, FileMode.OpenOrCreate, FileAccess.Write)) {
          tex.ToDDS(ddsStream);
        }
      }
    }
  }
}
