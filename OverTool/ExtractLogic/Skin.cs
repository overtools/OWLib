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
using System.Reflection;
using System.Linq;

namespace OverTool.ExtractLogic {
  class Skin {
    public static void FindTextures(ulong key, Dictionary<ulong, List<ImageLayer>> layers, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler) {
      ulong tgt = key;
      if(replace.ContainsKey(tgt)) {
        tgt = replace[tgt];
      }
      if(!map.ContainsKey(tgt)) {
        return;
      }
      if(!parsed.Add(tgt)) {
        return;
      }

      STUD record = new STUD(Util.OpenFile(map[tgt], handler));
      if(record.Instances[0] == null) {
        return;
      }
      MaterialMaster master = (MaterialMaster)record.Instances[0];
      if(master == null) {
        return;
      }
      foreach(MaterialMaster.MaterialMasterMaterial material in master.Materials) {
        ulong materialId = material.id;
        ulong materialKey = material.record.key;
        if(replace.ContainsKey(materialKey)) {
          materialKey = replace[materialKey];
        }
        if(!map.ContainsKey(materialKey)) {
          continue;
        }
        Material mat = new Material(Util.OpenFile(map[materialKey], handler));
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
          if(replace.ContainsKey(layer.key)) {
            layer.key = replace[layer.key];
          }
          layers[materialId].Add(layer);
        }
      }
    }

    private static void FindModels(ulong key, List<ulong> ignore, HashSet<ulong> models, Dictionary<ulong, List<ImageLayer>> layers, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler) {
      if(key == 0) {
        return;
      }
      ulong tgt = key;
      if(replace.ContainsKey(tgt)) {
        tgt = replace[tgt];
      }

      if(!map.ContainsKey(tgt)) {
        return;
      }
      if(!parsed.Add(tgt)) {
        return;
      }

      STUD record = new STUD(Util.OpenFile(map[tgt], handler), true, STUDManager.Instance, false, true);
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
          FindModels(bindingKey, ignore, models, layers, replace, parsed, map, handler);
        }
        if(inst.Name == record.Manager.GetName(typeof(ComplexModelRecord))) {
          ComplexModelRecord r = (ComplexModelRecord)inst;
          ulong modelKey = r.Data.model.key;
          if(replace.ContainsKey(modelKey)) {
            modelKey = replace[modelKey];
          }
          if(ignore.Count > 0 && !ignore.Contains(APM.keyToIndexID(modelKey))) {
            continue;
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
            FindModels(bindingKey, ignore, models, layers, replace, parsed, map, handler);
          }
        }
        if(inst.Name == record.Manager.GetName(typeof(ModelParamRecord))) {
          ModelParamRecord r = (ModelParamRecord)inst;
          ulong bindingKey = r.Param.binding.key;
          if(replace.ContainsKey(bindingKey)) {
            bindingKey = replace[bindingKey];
          }
          FindModels(bindingKey, ignore, models, layers, replace, parsed, map, handler);
        }
        if(inst.Name == record.Manager.GetName(typeof(ProjectileModelRecord))) {
          ProjectileModelRecord r = (ProjectileModelRecord)inst;
          foreach(ProjectileModelRecord.BindingRecord br in r.Children) {
            ulong bindingKey = br.binding.key;
            if(replace.ContainsKey(bindingKey)) {
              bindingKey = replace[bindingKey];
            }
            FindModels(bindingKey, ignore, models, layers, replace, parsed, map, handler);
          }
        }
        if(inst.Name == record.Manager.GetName(typeof(ChildParameterRecord))) {
          ChildParameterRecord r = (ChildParameterRecord)inst;
          foreach(ChildParameterRecord.Child br in r.Children) {
            ulong bindingKey = br.parameter.key;
            if(replace.ContainsKey(bindingKey)) {
              bindingKey = replace[bindingKey];
            }
            FindModels(bindingKey, ignore, models, layers, replace, parsed, map, handler);
          }
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
      if(record.Instances[0] == null) {
        return;
      }
      if(record.Instances[0].Name == record.Manager.GetName(typeof(TextureOverride))) {
        Dictionary<ulong, ulong> tmp = new Dictionary<ulong, ulong>();
        TextureOverride over = (TextureOverride)record.Instances[0];
        for(int i = 0; i < over.Replace.Length; ++i) {
          if(!map.ContainsKey(over.Target[i])) {
            continue;
          }
          if(tmp.ContainsKey(over.Replace[i])) {
            continue;
          }
          tmp[over.Replace[i]] = over.Target[i];
        }
        STUD overstud = new STUD(Util.OpenFile(map[over.Header.master.key], handler));
        TextureOverrideMaster overmaster = (TextureOverrideMaster)overstud.Instances[0];
        foreach(OWRecord rec in over.SubDefinitions) {
          FindReplacements(rec.key, tmp, parsed, map, handler);
        }
        if(overmaster.Header.offsetInfo > 0) {
          for(int i = 0; i < overmaster.Replace.Length; ++i) {
            if(!map.ContainsKey(overmaster.Target[i])) {
              continue;
            }
            if(tmp.ContainsKey(overmaster.Replace[i])) {
              continue;
            }
            tmp[over.Replace[i]] = overmaster.Target[i];
          }
        }
        foreach(KeyValuePair<ulong, ulong> kv in tmp) {
          if(overmaster.Header.modelOverride.key == 0) {
            ushort idx = (ushort)APM.keyToTypeID(kv.Value);
            if(idx != 0x0B3 && idx != 0x008 && idx != 0x004) {
              continue;
            }
          }
          replace[kv.Key] = kv.Value;
        }
      } else if(record.Instances[0].Name == record.Manager.GetName(typeof(TextureOverrideSecondary))) {
        TextureOverrideSecondary over = (TextureOverrideSecondary)record.Instances[0];
        for(int i = 0; i < over.Replace.Length; ++i) {
          if(!map.ContainsKey(over.Target[i])) {
            continue;
          }
          if(replace.ContainsKey(over.Replace[i])) {
            continue;
          }
          replace[over.Replace[i]] = over.Target[i];
        }
      }
    }

    public static void Extract(HeroMaster master, STUD itemStud, string output, string heroName, string itemName, List<ulong> ignore, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, List<char> furtherOpts) {
      string path = string.Format("{0}{1}{2}{1}{3}{1}{4}{1}", output, Path.DirectorySeparatorChar, Util.SanitizePath(heroName), Util.SanitizePath(itemStud.Instances[0].Name), Util.SanitizePath(itemName));

      SkinItem skin = (SkinItem)itemStud.Instances[0];

      HashSet<ulong> models = new HashSet<ulong>();
      HashSet<ulong> parsed = new HashSet<ulong>();
      Dictionary<ulong, List<ImageLayer>> layers = new Dictionary<ulong, List<ImageLayer>>();
      Dictionary<ulong, ulong> replace = new Dictionary<ulong, ulong>();
      if(itemName.ToLowerInvariant() != "classic") {
        FindReplacements(skin.Data.skin.key, replace, parsed, map, handler);
      }
      ulong bindingKey = master.Header.binding.key;
      if(replace.ContainsKey(bindingKey)) {
        bindingKey = replace[bindingKey];
      }
      FindModels(bindingKey, ignore, models, layers, replace, parsed, map, handler);
      bindingKey = master.Header.child1.key;
      if(replace.ContainsKey(bindingKey)) {
        bindingKey = replace[bindingKey];
      }
      FindModels(bindingKey, ignore, models, layers, replace, parsed, map, handler);
      bindingKey = master.Header.child2.key;
      if(replace.ContainsKey(bindingKey)) {
        bindingKey = replace[bindingKey];
      }
      FindModels(bindingKey, ignore, models, layers, replace, parsed, map, handler);
      bindingKey = master.Header.child3.key;
      if(replace.ContainsKey(bindingKey)) {
        bindingKey = replace[bindingKey];
      }
      FindModels(bindingKey, ignore, models, layers, replace, parsed, map, handler);
      bindingKey = master.Header.child4.key;
      if(replace.ContainsKey(bindingKey)) {
        bindingKey = replace[bindingKey];
      }
      FindModels(bindingKey, ignore, models, layers, replace, parsed, map, handler);
      
      foreach(KeyValuePair<ulong, List<ImageLayer>> kv in layers) {
        ulong materialId = kv.Key;
        List<ImageLayer> sublayers = kv.Value;
        foreach(ImageLayer layer in sublayers) {
          if(!parsed.Add(layer.key)) {
            continue;
          }
          SaveTexture(layer.key, map, handler, string.Format("{0}{1:X12}.dds", path, APM.keyToIndexID(layer.key)));
        }
      }

      IModelWriter writer = null;
      string mtlPath = null;
      if(furtherOpts.Count > 0) {
        Assembly asm = typeof(IModelWriter).Assembly;
        Type t = typeof(IModelWriter);
        List<Type> types = asm.GetTypes().Where(tt => tt != t && t.IsAssignableFrom(tt)).ToList();
        foreach(Type tt in types) {
          if(writer != null) {
            break;
          }
          if(tt.IsInterface) {
            continue;
          }

          IModelWriter tmp = (IModelWriter)Activator.CreateInstance(tt);
          for(int i = 0; i < tmp.Identifier.Length; ++i) {
            if(tmp.Identifier[i] == furtherOpts[0]) {
              writer = tmp;
              break;
            }
          }
        }
      }

      if(writer == null) {
        writer = new OWMDLWriter();
      }

      if(writer.GetType() == typeof(OWMDLWriter)) {
        IModelWriter tmp = new OWMATWriter();
        mtlPath = string.Format("{0}material{1}", path, tmp.Format);
        using(Stream outp = File.Open(mtlPath, FileMode.Create, FileAccess.Write)) {
          tmp.Write(null, outp, null, layers, new object[3] { false, Path.GetFileName(mtlPath), string.Format("{0} Skin {1}", heroName, itemName) });
          Console.Out.WriteLine("Wrote materials {0}", mtlPath);
        }
      } else if(writer.GetType() == typeof(OBJWriter)) {
        writer = new OBJWriter();
        IModelWriter tmp = new MTLWriter();
        mtlPath = string.Format("{0}material{1}", path, tmp.Format);
        using(Stream outp = File.Open(mtlPath, FileMode.Create, FileAccess.Write)) {
          tmp.Write(null, outp, null, layers, new object[3] { false, Path.GetFileName(mtlPath), string.Format("{0} Skin {1}", heroName, itemName) });
          Console.Out.WriteLine("Wrote materials {0}", mtlPath);
        }
      }

      List<byte> lods = new List<byte>(new byte[3] { 0, 1, 0xFF });
      foreach(ulong key in models) {
        if(!map.ContainsKey(key)) {
          continue;
        }
        Model mdl = new Model(Util.OpenFile(map[key], handler));
        string mdlName = string.Format("{0} Skin {1}_{2:X}", heroName, itemName, APM.keyToIndex(key));

        string outpath = string.Format("{0}{1:X12}{2}", path, APM.keyToIndexID(key), writer.Format);
        if(!Directory.Exists(Path.GetDirectoryName(output))) {
          Directory.CreateDirectory(Path.GetDirectoryName(output));
        }
        using(Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
          writer.Write(mdl, outp, lods, layers, new object[3] { true, Path.GetFileName(mtlPath), mdlName });
          Console.Out.WriteLine("Wrote model {0}", outpath);
        }
      }
    }

    public static void SaveTexture(ulong key, Dictionary<ulong, Record> map, CASCHandler handler, string path) {
      if(!map.ContainsKey(key)) {
        return;
      }
      
      if(!Directory.Exists(Path.GetDirectoryName(path))) {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
      }

      ulong imageDataKey = (key & 0xFFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL;
      bool dbl = map.ContainsKey(imageDataKey);

      using(Stream output = File.Open(path, FileMode.Create, FileAccess.Write)) {
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
