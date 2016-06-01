using System;
using System.IO;
using OWLib;
using System.Reflection;

namespace TextureTool {
  class Program {
    static void Main(string[] args) {
      if(args.Length < 3) {
        Console.Out.WriteLine("Usage: TextureTool.exe 004_file 04D_file output_file");
        return;
      }

      Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString());

      string headerFile = args[0];
      string dataFile = args[1];
      string destFile = args[2];

      using(Stream headerStream = File.Open(headerFile, FileMode.Open, FileAccess.Read))
      using(Stream dataStream = File.Open(dataFile, FileMode.Open, FileAccess.Read)) {
        Texture tex = new Texture(headerStream, dataStream);
        Console.Out.WriteLine("Opened Texture. W: {0} H: {1}", tex.Header.width, tex.Header.height);
        using(Stream ddsStream = File.Open(destFile, FileMode.OpenOrCreate, FileAccess.Write)) {
          tex.ToDDS(ddsStream);
          Console.Out.WriteLine("Saved DDS");
        }
      }
    }
  }
}
