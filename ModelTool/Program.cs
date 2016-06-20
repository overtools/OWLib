using System.IO;
using System;
using OWLib;
using System.Collections.Generic;
using System.Reflection;
using OWLib.ModelWriter;
using System.Linq;

namespace ModelTool {
  public class Program {
    private static List<IModelWriter> writers;

    public static void Main(string[] args) {

      writers = new List<IModelWriter>();

      Assembly asm = typeof(IModelWriter).Assembly;
      Type t = typeof(IModelWriter);
      List<Type> types = asm.GetTypes().Where(tt => tt != t && t.IsAssignableFrom(tt)).ToList();
      foreach(Type tt in types) {
        if(tt.IsInterface) {
          continue;
        }

        writers.Add((IModelWriter)Activator.CreateInstance(tt));
      }

      if(args.Length < 3) {
        Console.Out.WriteLine("Usage: ModelTool.exe 00C_file type [-l n] output_file");
        Console.Out.WriteLine("type can be:");
        Console.Out.WriteLine("  t - supprt - {0, -30} - normal extension", "name");
        Console.Out.WriteLine("".PadLeft(60, '-'));
        foreach(IModelWriter w in writers) {
          Console.Out.WriteLine("  {0} - {1} - {2,-30} - {3}", w.Identifier[0], SupportLevel(w.SupportLevel), w.Name, w.Format);
        }
        Console.Out.WriteLine("vutbpm = vertex / uv / attachment / bone / pose / material support");
        Console.Out.WriteLine("args:");
        Console.Out.WriteLine("  -l n - only save LOD, where N is lod");
        Console.Out.WriteLine("  -t   - save attachment points (sockets)");
        return;
      }

      Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString());

      string modelFile = args[0];
      char type = args[1][0];
      string outputFile = args[args.Length - 1];
      List<byte> lods = null;
      bool attachments = false;
      if(args.Length > 3) {
        int i = 2;
        while(i < args.Length - 2) {
          string arg = args[i];
          ++i;
          if(arg[0] == '-') {
            if(arg[1] == 'l') {
              if(lods == null) {
                lods = new List<byte>();
              }
              byte b = byte.Parse(args[i], System.Globalization.NumberStyles.Number);
              lods.Add(b);
              ++i;
            } else if(arg[1] == 't') {
              attachments = true;
            }
          } else {
            continue;
          }
        }
      }

      IModelWriter writer = null;
      foreach(IModelWriter w in writers) {
        if(w.Identifier.Contains(type)) {
          writer = w;
          break;
        }
      }
      if(writer == null) {
        Console.Error.WriteLine("Unsupported format {0}", type);
        return;
      }

      using(Stream modelStream = File.Open(modelFile, FileMode.Open, FileAccess.Read)) {
        Model model = new Model(modelStream);
        using(Stream outStream = File.Open(outputFile, FileMode.Create, FileAccess.Write)) {
          writer.Write(model, outStream, lods, new Dictionary<ulong, List<OWLib.Types.ImageLayer>>(), new bool[] { attachments });
        }
      }
    }

    private static string SupportLevel(ModelWriterSupport supportLevel) {
      char[] r = new char[6] {
        '.', '.', '.', '.', '.', '.'
      };
      
      if((supportLevel & ModelWriterSupport.VERTEX) == ModelWriterSupport.VERTEX) {
        r[0] = 'v';
      }
      if((supportLevel & ModelWriterSupport.UV) == ModelWriterSupport.UV) {
        r[1] = 'u';
      }
      if((supportLevel & ModelWriterSupport.ATTACHMENT) == ModelWriterSupport.ATTACHMENT) {
        r[2] = 't';
      }
      if((supportLevel & ModelWriterSupport.BONE) == ModelWriterSupport.BONE) {
        r[3] = 'b';
      }
      if((supportLevel & ModelWriterSupport.POSE) == ModelWriterSupport.POSE) {
        r[4] = 'p';
      }
      if((supportLevel & ModelWriterSupport.MATERIAL) == ModelWriterSupport.MATERIAL) {
        r[5] = 'm';
      }

      return new string(r);
    }
  }
}
