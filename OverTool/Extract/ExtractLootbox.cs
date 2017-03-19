using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;
using OWLib.Types;
using OWLib.Types.STUD.Binding;
using OverTool.ExtractLogic;
using OWLib.ModelWriter;

namespace OverTool.List {
  class ExtractLootbox : IOvertool {
    public string Help => "output";
    public uint MinimumArgs => 0;
    public char Opt => 'L';
    public string Title => "Extract Lootboxes";
    public ushort[] Track => new ushort[1] { 0xCF };

    public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
      Console.Out.WriteLine();
      foreach(ulong master in track[0xCF]) {
        if(!map.ContainsKey(master)) {
          continue;
        }
        STUD lootbox = new STUD(Util.OpenFile(map[master], handler));
        Lootbox box = lootbox.Instances[0] as Lootbox;
        if(box == null) {
          continue;
        }

        Extract(box.Master.model, box, track, map, handler, args);
        Extract(box.Master.alternate, box, track, map, handler, args);
      }
    }

    private void Extract(ulong model, Lootbox lootbox, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
      if(model == 0 || !map.ContainsKey(model)) {
        return;
      }

      string output = $"{args[0]}{Path.DirectorySeparatorChar}{Util.SanitizePath(lootbox.EventName)}{Path.DirectorySeparatorChar}";

      STUD stud = new STUD(Util.OpenFile(map[model], handler));

      HashSet<ulong> models = new HashSet<ulong>();
      Dictionary<ulong, ulong> animList = new Dictionary<ulong, ulong>();
      HashSet<ulong> parsed = new HashSet<ulong>();
      Dictionary<ulong, List<ImageLayer>> layers = new Dictionary<ulong, List<ImageLayer>>();
      Dictionary<ulong, ulong> replace = new Dictionary<ulong, ulong>();

      foreach(ISTUDInstance inst in stud.Instances) {
        if(inst == null) {
          continue;
        }
        if(inst.Name == stud.Manager.GetName(typeof(ComplexModelRecord))) {
          ComplexModelRecord r = (ComplexModelRecord)inst;
          ulong modelKey = r.Data.model.key;
          models.Add(modelKey);
          Skin.FindAnimations(r.Data.animationList.key, animList, replace, parsed, map, handler, models, layers, modelKey);
          Skin.FindAnimations(r.Data.secondaryAnimationList.key, animList, replace, parsed, map, handler, models, layers, modelKey);
          Skin.FindTextures(r.Data.material.key, layers, replace, parsed, map, handler);
        }
      }

      // TODO: genericify
      IModelWriter mod = new OWMDLWriter();
      IModelWriter mat = new OWMATWriter();
      IModelWriter refpose = new RefPoseWriter();
      string matPath = $"{output}material{mat.Format}";

      Dictionary<string, TextureType> typeInfo = new Dictionary<string, TextureType>();
      foreach(KeyValuePair<ulong, List<ImageLayer>> kv in layers) {
        ulong materialId = kv.Key;
        List<ImageLayer> sublayers = kv.Value;
        foreach(ImageLayer layer in sublayers) {
          if(!parsed.Add(layer.key)) {
            continue;
          }
          KeyValuePair<string, TextureType> stt = Skin.SaveTexture(layer.key, map, handler, $"{output}{GUID.Attribute(layer.key, GUID.AttributeEnum.Index | GUID.AttributeEnum.Locale | GUID.AttributeEnum.Region | GUID.AttributeEnum.Platform):X12}.dds");
          typeInfo.Add(stt.Key, stt.Value);
        }
      }
      using(Stream outp = File.Open(matPath, FileMode.Create, FileAccess.Write)) {
        if(mat.Write(null, outp, null, layers, new object[3] { typeInfo, Path.GetFileName(matPath), $"{lootbox.EventNameNormal} {GUID.Index(model):X}" })) {
          Console.Out.WriteLine("Wrote materials {0}", matPath);
        } else {
          Console.Out.WriteLine("Failed to write material");
        }
      }
      foreach(KeyValuePair<ulong, ulong> kv in animList) {
        ulong parent = kv.Value;
        ulong key = kv.Key;
        string outpath = string.Format("{0}Animations{1}{2:X12}{1}{3:X12}.{4:X3}", output, Path.DirectorySeparatorChar, GUID.Index(parent), GUID.Attribute(key, GUID.AttributeEnum.Index | GUID.AttributeEnum.Locale | GUID.AttributeEnum.Region | GUID.AttributeEnum.Platform), GUID.Type(key));
        if(!Directory.Exists(Path.GetDirectoryName(outpath))) {
          Directory.CreateDirectory(Path.GetDirectoryName(outpath));
        }
        using(Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
          Stream anim = Util.OpenFile(map[key], handler);
          if(output != null) {
            anim.CopyTo(outp);
            Console.Out.WriteLine("Wrote animation {0}", outpath);
            anim.Close();
          }
        }
      }
      List<byte> lods = new List<byte>(new byte[3] { 0, 1, 0xFF });
      foreach(ulong key in models) {
        if(!map.ContainsKey(key)) {
          continue;
        }

        string outpath;

        if(!Directory.Exists(Path.GetDirectoryName(output))) {
          Directory.CreateDirectory(Path.GetDirectoryName(output));
        }

        outpath = $"{output}{GUID.Attribute(key, GUID.AttributeEnum.Index | GUID.AttributeEnum.Locale | GUID.AttributeEnum.Region | GUID.AttributeEnum.Platform):X12}.{GUID.Type(key):X3}";

        using(Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
          Util.OpenFile(map[key], handler).CopyTo(outp);
          Console.Out.WriteLine("Wrote raw model {0}", outpath);
        }

        Chunked mdl = new Chunked(Util.OpenFile(map[key], handler));
        
        outpath = $"{output}{GUID.Attribute(key, GUID.AttributeEnum.Index | GUID.AttributeEnum.Locale | GUID.AttributeEnum.Region | GUID.AttributeEnum.Platform):X12}_refpose{refpose.Format}";
        using(Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
          if(refpose.Write(mdl, outp, null, null, null)) {
            Console.Out.WriteLine("Wrote reference pose {0}", outpath);
          }
        }

        string mdlName = $"{lootbox.EventNameNormal} {GUID.Index(key):X}";

        outpath = $"{output}{GUID.Attribute(key, GUID.AttributeEnum.Index | GUID.AttributeEnum.Locale | GUID.AttributeEnum.Region | GUID.AttributeEnum.Platform):X12}{mod.Format}";

        using(Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
          if(mod.Write(mdl, outp, lods, layers, new object[5] { true, Path.GetFileName(matPath), mdlName, null, true })) {
            Console.Out.WriteLine("Wrote model {0}", outpath);
          } else {
            Console.Out.WriteLine("Failed to write model");
          }
        }
      }
    }
  }
}