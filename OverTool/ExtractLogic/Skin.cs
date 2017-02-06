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
using OWLib.Types.Chunk;

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
      if(record.Instances.Length == 0) {
        return;
      }
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

    private static void FindAnimationsSoft(ulong key, Dictionary<ulong, ulong> animList, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler, HashSet<ulong> models, Dictionary<ulong, List<ImageLayer>> layers, ulong parent = 0) {
      if(!map.ContainsKey(key)) {
        return;
      }
      if(APM.keyToTypeID(key) != 0x006) {
        return;
      }
      if(!parsed.Add(key)) {
        return;
      }

      using(Stream anim = Util.OpenFile(map[key], handler)) {
        if(anim == null) {
          return;
        }
        using(BinaryReader reader = new BinaryReader(anim)) {
          anim.Position = 0x18L;
          ulong infokey = reader.ReadUInt64();
          if(infokey == 0) {
            return;
          }
          if(replace.ContainsKey(infokey)) {
            infokey = replace[infokey];
          }
          if(!map.ContainsKey(infokey)) {
            return;
          }
          if(APM.keyToTypeID(infokey) != 0x08F) {
            return;
          }
          if(!parsed.Add(infokey)) {
            return;
          }
          Chunked chunked = new Chunked(Util.OpenFile(map[key], handler));
          DMCE[] chunks = chunked.GetAllOfTypeFlat<DMCE>();
          foreach(DMCE dmce in chunks) {
            if(models != null) {
              models.Add(dmce.Data.modelKey);
            }
            if(layers != null) {
              FindTextures(dmce.Data.materialKey, layers, replace, parsed, map, handler);
            }
            if(animList != null && !animList.ContainsKey(dmce.Data.animationKey)) {
              animList[dmce.Data.animationKey] = parent;
            }
          }
        }
      }
    }

    public static void FindAnimations(ulong key, Dictionary<ulong, ulong> animList, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler, HashSet<ulong> models, Dictionary<ulong, List<ImageLayer>> layers, ulong parent = 0) {
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
      if(record.Instances == null) {
        return;
      }
      foreach(ISTUDInstance inst in record.Instances) {
        if(inst == null) {
          continue;
        }
        if(inst.Name == record.Manager.GetName(typeof(VictoryPoseItem))) {
          VictoryPoseItem item = (VictoryPoseItem)inst;
          FindAnimations(item.Data.f0BF.key, animList, replace, parsed, map, handler, models, layers);
        } else if(inst.Name == record.Manager.GetName(typeof(AnimationList))) {
          AnimationList r = (AnimationList)inst;
          foreach(AnimationList.AnimationListEntry entry in r.Entries) {
            ulong bindingKey = entry.animation.key;
            if(replace.ContainsKey(bindingKey)) {
              bindingKey = replace[bindingKey];
            }
            if(!map.ContainsKey(bindingKey)) {
              continue;
            }
            if(animList.ContainsKey(bindingKey) && animList[bindingKey] > 0) {
              continue;
            }
            animList[bindingKey] = parent;
            FindAnimationsSoft(bindingKey, animList, replace, parsed, map, handler, models, layers, bindingKey);
          }
        }
        if(inst.Name == record.Manager.GetName(typeof(Pose))) {
          Pose r = (Pose)inst;
          foreach(OWRecord animation in new OWRecord[3] { r.Header.animation1, r.Header.animation2, r.Header.animation3 }) {
            ulong bindingKey = animation.key;
            if(replace.ContainsKey(bindingKey)) {
              bindingKey = replace[bindingKey];
            }
            if(!map.ContainsKey(bindingKey)) {
              continue;
            }
            if(animList.ContainsKey(bindingKey) && animList[bindingKey] > 0) {
              continue;
            }
            animList[bindingKey] = parent;
            FindAnimationsSoft(bindingKey, animList, replace, parsed, map, handler, models, layers, bindingKey);
          }
        }
        if(inst.Name == record.Manager.GetName(typeof(AnimationListInfo))) {
          AnimationListInfo r = (AnimationListInfo)inst;
          foreach(AnimationListInfo.AnimationListEntry entry in r.Entries) {
            FindAnimations(entry.secondary.key, animList, replace, parsed, map, handler, models, layers, parent);
          }
        }
        if(inst.Name == record.Manager.GetName(typeof(AnimationListReference))) {
          AnimationListReference r = (AnimationListReference)inst;
          foreach(OWRecord animation in new OWRecord[5] { r.Header.unkD, r.Header.animation, r.Header.unk12, r.Header.unk15, r.Header.unk18}) {
            ulong bindingKey = animation.key;
            if(replace.ContainsKey(bindingKey)) {
              bindingKey = replace[bindingKey];
            }
            if(!map.ContainsKey(bindingKey)) {
              continue;
            }
            if(animList.ContainsKey(bindingKey) && animList[bindingKey] > 0) {
              continue;
            }
            ulong keyid = APM.keyToTypeID(bindingKey);
            if(keyid == 0x6) {
              animList[bindingKey] = parent;
              FindAnimationsSoft(bindingKey, animList, replace, parsed, map, handler, models, layers, bindingKey);
            } else if(keyid == 0x20 || keyid == 0x21) {
              FindAnimations(bindingKey, animList, replace, parsed, map, handler, models, layers, parent);
            }
          }
        }
      }
    }

    public static void FindModels(ulong key, List<ulong> ignore, HashSet<ulong> models, Dictionary<ulong, ulong> animList, Dictionary<ulong, List<ImageLayer>> layers, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler) {
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

      switch(APM.keyToTypeID(tgt)) {
        case 0xC: // model
          models.Add(tgt);
          return;
        case 0x1A: // texture
          FindTextures(tgt, layers, replace, parsed, map, handler);
          return;
        case 0x20: // animation
        case 0x21:
          FindAnimations(tgt, animList, replace, parsed, map, handler, models, layers, 0);
          return;
        case 0x7C: // string
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
          FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler);
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
          FindAnimations(r.Data.animationList.key, animList, replace, parsed, map, handler, models, layers, modelKey);
          FindAnimations(r.Data.secondaryAnimationList.key, animList, replace, parsed, map, handler, models, layers, modelKey);
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
            FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler);
          }
        }
        if(inst.Name == record.Manager.GetName(typeof(BindingRecord))) {
          BindingRecord r = (BindingRecord)inst;
          ulong bindingKey = r.Param.binding.key;
          if(replace.ContainsKey(bindingKey)) {
            bindingKey = replace[bindingKey];
          }
          FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler);
          bindingKey = r.Param.binding2.key;
          if(replace.ContainsKey(bindingKey)) {
            bindingKey = replace[bindingKey];
          }
          FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler);
        }
        if(inst.Name == record.Manager.GetName(typeof(ChildGameParameterRecord))) {
          ChildGameParameterRecord r = (ChildGameParameterRecord)inst;
          ulong bindingKey = r.Param.binding.key;
          if(replace.ContainsKey(bindingKey)) {
            bindingKey = replace[bindingKey];
          }
          FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler);
        }
        if(inst.Name == record.Manager.GetName(typeof(SubModelRecord))) {
          SubModelRecord r = (SubModelRecord)inst;
          foreach(SubModelRecord.SubModelEntry entry in r.Entries) {
            ulong bindingKey = entry.binding;
            if(replace.ContainsKey(bindingKey)) {
              bindingKey = replace[bindingKey];
            }
            FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler);
          }
        }
        if(inst.Name == record.Manager.GetName(typeof(ChildParameterRecord))) {
          ChildParameterRecord r = (ChildParameterRecord)inst;
          foreach(ChildParameterRecord.Child br in r.Children) {
            ulong bindingKey = br.parameter.key;
            if(replace.ContainsKey(bindingKey)) {
              bindingKey = replace[bindingKey];
            }
            FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler);
          }
        }
        if(inst.Name == record.Manager.GetName(typeof(AnimationCoreference))) {
          AnimationCoreference r = (AnimationCoreference)inst;
          foreach(AnimationCoreference.AnimationCoreferenceEntry entry in r.Entries) {
            ulong bindingKey = entry.animation.key;
            if(replace.ContainsKey(bindingKey)) {
              bindingKey = replace[bindingKey];
            }
            if(!map.ContainsKey(bindingKey)) {
              continue;
            }
            if(animList.ContainsKey(bindingKey) && animList[bindingKey] > 0) {
              continue;
            }
            animList[bindingKey] = 0;
            FindAnimationsSoft(bindingKey, animList, replace, parsed, map, handler, models, layers, bindingKey);
          }
        }
        if(inst.Name == record.Manager.GetName(typeof(PoseList))) {
          PoseList r = (PoseList)inst;
          if(r.Header.reference.key != 0) {
            FindAnimations(r.Header.reference.key, animList, replace, parsed, map, handler, models, layers, 0);
          }
        }
      }
    }

    private static void FindReplacements(ulong key, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler, HeroMaster master, SkinItem skin) {
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
        TextureOverride over = (TextureOverride)record.Instances[0];
        for(int i = 0; i < over.Replace.Length; ++i) {
          if(!map.ContainsKey(over.Target[i])) {
            continue;
          }
          if(replace.ContainsKey(over.Replace[i])) {
            continue;
          }
          replace[over.Replace[i]] = over.Target[i];
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

    public static void Extract(HeroMaster master, STUD itemStud, string output, string heroName, string itemName, string itemGroup, List<ulong> ignore, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, List<char> furtherOpts) {
      string path = string.Format("{0}{1}{2}{1}{3}{1}{5}{1}{4}{1}", output, Path.DirectorySeparatorChar, Util.Strip(Util.SanitizePath(heroName)), Util.SanitizePath(itemStud.Instances[0].Name), Util.SanitizePath(itemName), Util.SanitizePath(itemGroup));

      SkinItem skin = (SkinItem)itemStud.Instances[0];

      HashSet<ulong> models = new HashSet<ulong>();
      Dictionary<ulong, ulong> animList = new Dictionary<ulong, ulong>();
      HashSet<ulong> parsed = new HashSet<ulong>();
      Dictionary<ulong, List<ImageLayer>> layers = new Dictionary<ulong, List<ImageLayer>>();
      Dictionary<ulong, ulong> replace = new Dictionary<ulong, ulong>();

      FindReplacements(skin.Data.skin.key, replace, parsed, map, handler, master, skin);

      ulong bindingKey = master.Header.binding.key;
      if(replace.ContainsKey(bindingKey)) {
        bindingKey = replace[bindingKey];
      }
      FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler);
      bindingKey = master.Header.child1.key;
      if(replace.ContainsKey(bindingKey)) {
        bindingKey = replace[bindingKey];
      }
      FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler);
      bindingKey = master.Header.child2.key;
      if(replace.ContainsKey(bindingKey)) {
        bindingKey = replace[bindingKey];
      }
      FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler);
      bindingKey = master.Header.child3.key;
      if(replace.ContainsKey(bindingKey)) {
        bindingKey = replace[bindingKey];
      }
      FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler);
      bindingKey = master.Header.child4.key;
      if(replace.ContainsKey(bindingKey)) {
        bindingKey = replace[bindingKey];
      }
      FindModels(bindingKey, ignore, models, animList, layers, replace, parsed, map, handler);

      Save(master, path, heroName, itemName, replace, parsed, models, layers, animList, furtherOpts, track, map, handler);
    }

    public static void Save(HeroMaster master, string path, string heroName, string itemName, Dictionary<ulong, ulong> replace, HashSet<ulong> parsed, HashSet<ulong> models, Dictionary<ulong, List<ImageLayer>> layers, Dictionary<ulong, ulong> animList, List<char> furtherOpts, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler) {
      Dictionary<string, TextureType> typeInfo = new Dictionary<string, TextureType>();
      if(furtherOpts.Count < 2 || furtherOpts[1] != 'T') {
        foreach(KeyValuePair<ulong, List<ImageLayer>> kv in layers) {
          ulong materialId = kv.Key;
          List<ImageLayer> sublayers = kv.Value;
          foreach(ImageLayer layer in sublayers) {
            if(!parsed.Add(layer.key)) {
              continue;
            }
            KeyValuePair<string, TextureType> stt = SaveTexture(layer.key, map, handler,
                $"{path}{APM.keyToIndexID(layer.key):X12}.dds");
            typeInfo.Add(stt.Key, stt.Value);
          }
        }
      }

      IModelWriter writer = null;
      string mtlPath = null;
      if(furtherOpts.Count > 0) {
        if(furtherOpts[0] != '+') {
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
      }


      if(writer == null) {
        writer = new OWMDLWriter();
      }

      if(furtherOpts.Count < 2 || furtherOpts[1] != 'T') {
        if(writer.GetType() == typeof(OWMDLWriter) || furtherOpts[0] == '+') {
          IModelWriter tmp = new OWMATWriter();
          mtlPath = $"{path}material{tmp.Format}";
          using(Stream outp = File.Open(mtlPath, FileMode.Create, FileAccess.Write)) {
            if(tmp.Write(null, outp, null, layers, new object[3] { typeInfo, Path.GetFileName(mtlPath),
                $"{heroName} Skin {itemName}"
            })) {
              Console.Out.WriteLine("Wrote materials {0}", mtlPath);
            } else {
              Console.Out.WriteLine("Failed to write material");
            }
          }
        } else if(writer.GetType() == typeof(OBJWriter)) {
          writer = new OBJWriter();
          IModelWriter tmp = new MTLWriter();
          mtlPath = $"{path}material{tmp.Format}";
          using(Stream outp = File.Open(mtlPath, FileMode.Create, FileAccess.Write)) {
            if(tmp.Write(null, outp, null, layers, new object[3] { false, Path.GetFileName(mtlPath),
                $"{heroName} Skin {itemName}"
            })) {
              Console.Out.WriteLine("Wrote materials {0}", mtlPath);
            } else {
              Console.Out.WriteLine("Failed to write material");
            }
          }
        }
      }

      IModelWriter refpose = new RefPoseWriter();

      bool skipCmodel = true;

      if(furtherOpts.Count > 5 && furtherOpts[5] == 'C') {
        skipCmodel = false;
      }

      if(furtherOpts.Count < 4 || furtherOpts[3] != 'M') {
        List<byte> lods = new List<byte>(new byte[3] { 0, 1, 0xFF });
        foreach(ulong key in models) {
          if(!map.ContainsKey(key)) {
            continue;
          }

          string outpath;

          if(!Directory.Exists(Path.GetDirectoryName(path))) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
          }

          outpath = $"{path}{APM.keyToIndexID(key):X12}.{APM.keyToTypeID(key):X3}";

          using(Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
            Util.OpenFile(map[key], handler).CopyTo(outp);
            Console.Out.WriteLine("Wrote raw model {0}", outpath);
          }

          if(furtherOpts.Count > 0 && furtherOpts[0] == '+') { // raw
            continue;
          }
          
          Chunked mdl = new Chunked(Util.OpenFile(map[key], handler));

          if(furtherOpts.Count <= 6 || furtherOpts[6] != 'R') {
            outpath = $"{path}{APM.keyToIndexID(key):X12}_refpose{refpose.Format}";
            using(Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
              if(refpose.Write(mdl, outp, null, null, null)) {
                Console.Out.WriteLine("Wrote reference pose {0}", outpath);
              }
            }
          }

          string mdlName = $"{heroName} Skin {itemName}_{APM.keyToIndex(key):X}";

          outpath = $"{path}{APM.keyToIndexID(key):X12}{writer.Format}";

          using(Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
            if(writer.Write(mdl, outp, lods, layers, new object[5] { true, Path.GetFileName(mtlPath), mdlName, null, skipCmodel })) {
              Console.Out.WriteLine("Wrote model {0}", outpath);
            } else {
              Console.Out.WriteLine("Failed to write model");
            }
          }
        }
      }

      if(furtherOpts.Count < 3 || furtherOpts[2] != 'A') {
        foreach(KeyValuePair<ulong, ulong> kv in animList) {
          ulong parent = kv.Value;
          ulong key = kv.Key;
          string outpath = string.Format("{0}Animations{1}{2:X12}{1}{3:X12}.{4:X3}", path, Path.DirectorySeparatorChar, APM.keyToIndex(parent), APM.keyToIndexID(key), APM.keyToTypeID(key));
          if(!Directory.Exists(Path.GetDirectoryName(outpath))) {
            Directory.CreateDirectory(Path.GetDirectoryName(outpath));
          }
          using(Stream outp = File.Open(outpath, FileMode.Create, FileAccess.Write)) {
            Stream output = Util.OpenFile(map[key], handler);
            if(output != null) {
              output.CopyTo(outp);
              Console.Out.WriteLine("Wrote animation {0}", outpath);
              output.Close();
            }
          }
        }
      }

      if(furtherOpts.Count < 5 || furtherOpts[4] != 'S') {
        Console.Out.WriteLine("Dumping voice bites for hero {0} with skin {1}", heroName, itemName);
        List<ulong> soundData = Sound.FindSounds(master, track, map, handler, replace);
        string outpath = $"{path}Sound{Path.DirectorySeparatorChar}";
        if(!Directory.Exists(outpath)) {
          Directory.CreateDirectory(outpath);
        }
        DumpVoice.Save(outpath, soundData, map, handler, replace);
      }
    }

    public static KeyValuePair<string, TextureType> SaveTexture(ulong key, Dictionary<ulong, Record> map, CASCHandler handler, string path) {
      string name = $"{APM.keyToIndexID(key):X12}.dds";
      TextureType @type = TextureType.Unknown;

      if(!map.ContainsKey(key)) {
        return new KeyValuePair<string, TextureType>(name, @type);
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
          @type = tex.Format;
        } else {
          TextureLinear tex = new TextureLinear(Util.OpenFile(map[key], handler));
          tex.Save(output);
          @type = tex.Header.Format();
        }
      }
      Console.Out.WriteLine("Wrote texture {0}", path);
      return new KeyValuePair<string, TextureType>(name, @type);
    }
  }
}
