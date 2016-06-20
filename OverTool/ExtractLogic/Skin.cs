using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.Binding;
using OWLib.Types.STUD.GameParam;
using OWLib.Types.STUD.InventoryItem;
using OWLib.ModelWriter;

namespace OverTool.ExtractLogic {
  class Skin {
    private static void FindTextures(ulong key, Dictionary<ulong, List<ImageLayer>> layers, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler) {
      if(!map.ContainsKey(key)) {
        return;
      }
      if(!parsed.Add(key)) {
        return;
      }

      STUD record = new STUD(Util.OpenFile(map[key], handler));
      MaterialMaster master = (MaterialMaster)record.Instances[0];
      foreach(MaterialMaster.MaterialMasterMaterial material in master.Materials) {
        ulong materialId = material.id;
        ulong materialKey = material.record.key;
        if(replace.ContainsKey(materialKey)) {
          materialKey = replace[materialKey];
        }
        if(!map.ContainsKey(material.record.key)) {
          continue;
        }
        Material mat = new Material(Util.OpenFile(map[material.record.key], handler));
        ulong definitionKey = mat.Header.definitionKey;
        if(replace.ContainsKey(definitionKey)) {
          definitionKey = replace[definitionKey];
        }
        if(!map.ContainsKey(definitionKey)) {
          continue;
        }
        ImageDefinition def = new ImageDefinition(Util.OpenFile(map[definitionKey], handler));
        if(!layers.ContainsKey(materialId)) {
          layers.Add(materialId, new List<ImageLayer>());
        }
        for(int i = 0; i < def.Layers.Length; ++i) {
          ImageLayer layer = def.Layers[i];
          if(!replace.ContainsKey(layer.key)) {
            continue;
          }
          layer.key = replace[layer.key];
          def.Layers[i] = layer;
        }
        layers[materialId].AddRange(def.Layers);
      }
    }

    private static void FindModels(ulong key, HashSet<ulong> models, Dictionary<ulong, List<ImageLayer>> layers, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler) {
      if(!map.ContainsKey(key)) {
        return;
      }
      if(!parsed.Add(key)) {
        return;
      }

      STUD record = new STUD(Util.OpenFile(map[key], handler), true, STUDManager.Instance, false, true);
      foreach(ISTUDInstance inst in record.Instances) {
        if(inst == null) {
          continue;
        }
        if(inst.Name == record.Manager.GetName(typeof(ViewModelRecord))) {
          ViewModelRecord r = (ViewModelRecord)inst;
          ulong bindingKey = r.Data.binding.key;
          if(replace.ContainsKey(bindingKey)) {
            bindingKey = replace[bindingKey];
          }
          FindModels(bindingKey, models, layers, replace, parsed, map, handler);
        }
        if(inst.Name == record.Manager.GetName(typeof(ComplexModelRecord))) {
          ComplexModelRecord r = (ComplexModelRecord)inst;
          ulong modelKey = r.Data.model.key;
          if(replace.ContainsKey(modelKey)) {
            modelKey = replace[modelKey];
          }
          models.Add(modelKey);
          ulong target = r.Data.material.key;
          if(replace.ContainsKey(target)) {
            target = replace[target];
          }
          FindTextures(target, layers, replace, parsed, map, handler);
        }
        if(inst.Name == record.Manager.GetName(typeof(ParameterRecord))) {
          ParameterRecord r = (ParameterRecord)inst;
          foreach(ParameterRecord.ParameterEntry entry in r.Parameters) {
            ulong bindingKey = entry.parameter.key;
            if(replace.ContainsKey(bindingKey)) {
              bindingKey = replace[bindingKey];
            }
            FindModels(bindingKey, models, layers, replace, parsed, map, handler);
          }
        }
        if(inst.Name == record.Manager.GetName(typeof(ModelParamRecord))) {
          ModelParamRecord r = (ModelParamRecord)inst;
          ulong bindingKey = r.Param.binding.key;
          if(replace.ContainsKey(bindingKey)) {
            bindingKey = replace[bindingKey];
          }
          FindModels(bindingKey, models, layers, replace, parsed, map, handler);
        }
      }
    }

    private static void FindReplacements(ulong key, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler) {
      if(!map.ContainsKey(key)) {
        return;
      }
      if(!parsed.Add(key)) {
        return;
      }

      STUD record = new STUD(Util.OpenFile(map[key], handler));
      if(record.Instances[0].Name == record.Manager.GetName(typeof(TextureOverride))) {
        TextureOverride over = (TextureOverride)record.Instances[0];
        for(int i = 0; i < over.Replace.Length; ++i) {
          if(!map.ContainsKey(over.Target[i])) {
            continue;
          }
          replace[over.Replace[i]] = over.Target[i];
        }
        foreach(OWRecord rec in over.SubDefinitions) {
          FindReplacements(rec.key, replace, parsed, map, handler);
        }
      } else if(record.Instances[0].Name == record.Manager.GetName(typeof(TextureOverrideSecondary))) {
        TextureOverrideSecondary over = (TextureOverrideSecondary)record.Instances[0];
        for(int i = 0; i < over.Replace.Length; ++i) {
          if(!map.ContainsKey(over.Target[i])) {
            continue;
          }
          replace[over.Replace[i]] = over.Target[i];
        }
      }
    }

    public static void Extract(HeroMaster master, STUD itemStud, string output, string heroName, string itemName, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler) {
      string path = string.Format("{0}{1}{2}{1}{3}{1}{4}{1}", output, Path.DirectorySeparatorChar, Util.SanitizePath(heroName), Util.SanitizePath(itemStud.Instances[0].Name), Util.SanitizePath(itemName));

      SkinItem skin = (SkinItem)itemStud.Instances[0];

      HashSet<ulong> models = new HashSet<ulong>();
      HashSet<ulong> parsed = new HashSet<ulong>();
      Dictionary<ulong, List<ImageLayer>> layers = new Dictionary<ulong, List<ImageLayer>>();
      Dictionary<ulong, ulong> replace = new Dictionary<ulong, ulong>();

      FindReplacements(skin.Data.skin.key, replace, parsed, map, handler);
      ulong bindingKey = master.Header.binding.key;
      if(replace.ContainsKey(bindingKey)) {
        bindingKey = replace[bindingKey];
      }
      FindModels(bindingKey, models, layers, replace, parsed, map, handler);
      
      foreach(KeyValuePair<ulong, List<ImageLayer>> kv in layers) {
        ulong materialId = kv.Key;
        List<ImageLayer> sublayers = kv.Value;
        foreach(ImageLayer layer in sublayers) {
          if(!parsed.Add(layer.key)) {
            continue;
          }
          SaveTexture(layer.key, map, handler, string.Format("{0}{1:X16}_{2:X8}.dds", path, materialId, layer.unk));
        }
      }

      BINWriter writer = new BINWriter();
      List<byte> lods = new List<byte>(new byte[3] { 0, 1, 0xFF });
      foreach(ulong key in models) {
        if(!map.ContainsKey(key)) {
          continue;
        }
        Model mdl = new Model(Util.OpenFile(map[key], handler));

        string outpath = string.Format("{0}{1:X12}.xps", path, APM.keyToIndexID(key));
        if(!Directory.Exists(Path.GetDirectoryName(output))) {
          Directory.CreateDirectory(Path.GetDirectoryName(output));
        }
        using(Stream outp = File.Open(outpath, FileMode.OpenOrCreate, FileAccess.Write)) {
          writer.Write(mdl, outp, lods, layers, new bool[1] { false });
          Console.Out.WriteLine("Wrote model {0}", outpath);
        }
      }
    }

    private static void SaveTexture(ulong key, Dictionary<ulong, Record> map, CASCHandler handler, string path) {
      if(!map.ContainsKey(key)) {
        return;
      }
      
      if(!Directory.Exists(Path.GetDirectoryName(path))) {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
      }

      ulong imageDataKey = (key & 0xFFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL;
      bool dbl = map.ContainsKey(imageDataKey);

      using(Stream output = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write)) {
        if(map.ContainsKey(imageDataKey)) {
          Texture tex = new Texture(Util.OpenFile(map[key], handler), Util.OpenFile(map[imageDataKey], handler));
          tex.Save(output);
        } else {
          TextureLinear tex = new TextureLinear(Util.OpenFile(map[key], handler));
          tex.Save(output);
        }
      }
      Console.Out.WriteLine("Wrote texture {0}", path);
    }
  }
}
