using System;
using System.IO;
using OWLib;

namespace STUDTool {
  class MainClass {
    public static void Main(string[] args) {
      if(args.Length < 1) {
        Console.Out.WriteLine("Usage: STUDTool.exe STUDFile");
        return;
      }

      string file = args[0];

      STUDManager manager = STUDManager.Create();
      using(Stream stream = File.Open(file, FileMode.Open, FileAccess.Read)) {
        STUD stud = new STUD(manager, stream);
        stud.Dump();
      }
    }
  }
}
