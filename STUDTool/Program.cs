using System;
using System.IO;
using System.Reflection;
using OWLib;

namespace STUDTool {
  class MainClass {
    public static void Main(string[] args) {
      if(args.Length < 1) {
        Console.Out.WriteLine("Usage: STUDTool.exe STUDFile");
        return;
      }
      
      Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString());

      string file = args[0];

      Console.Out.WriteLine("Opening file {0}", Path.GetFileName(file));
      
      using(Stream stream = File.Open(file, FileMode.Open, FileAccess.Read)) {
        STUD stud = new STUD(stream);
        try {
          System.Diagnostics.Debugger.Break();
        } catch {
          Console.Error.WriteLine(file);
        }
      }
    }
  }
}
