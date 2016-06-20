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
    private static void FindTextures(ulong key, Dictionary<ulong, Stream> textures, Dictionary<ulong, ImageLayer> layers, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler) {
      if(!map.ContainsKey(key)) {
        return;
      }
      if(!parsed.Add(key)) {
        return;
      }
    }

    private static void FindModels(ulong key, HashSet<ulong> models, Dictionary<ulong, Stream> textures, Dictionary<ulong, ImageLayer> layers, HashSet<ulong> parsed, Dictionary<ulong, Record> map, CASCHandler handler) {
      if(!map.ContainsKey(key)) {
        return;
      }
      if(!parsed.Add(key)) {
        return;
      }

      STUD record = new STUD(Util.OpenFile(map[key], handler));
      foreach(ISTUDInstance inst in record.Instances) {
        if(inst == null) {
          continue;
        }
        if(inst.Name == "Binding:ViewModel") {
          ViewModelRecord r = (ViewModelRecord)inst;
          FindModels(r.Data.binding.key, models, textures, layers, parsed, map, handler);
        }
        if(inst.Name == "Binding:ComplexModel") {
          ComplexModelRecord r = (ComplexModelRecord)inst;
          models.Add(r.Data.model.key);
          FindTextures(r.Data.material.key, textures, layers, parsed, map, handler);
        }
        if(inst.Name == "Binding:GameParameter") {
          ParameterRecord r = (ParameterRecord)inst;
          foreach(ParameterRecord.ParameterEntry entry in r.Parameters) {
            FindModels(entry.parameter.key, models, textures, layers, parsed, map, handler);
          }
        }
        if(inst.Name == "GameParameter:Model") {
          ModelParamRecord r = (ModelParamRecord)inst;
          FindModels(r.Param.binding.key, models, textures, layers, parsed, map, handler);
        }
      }
    }

    public static void Extract(HeroMaster master, STUD itemStud, string output, string heroName, string itemName, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler) {
      string path = string.Format("{0}{1}{2}{1}{3}{1}{4}{1}", output, Path.DirectorySeparatorChar, Util.SanitizePath(heroName), Util.SanitizePath(itemStud.Instances[0].Name), Util.SanitizePath(itemName));

      SkinItem skin = (SkinItem)itemStud.Instances[0];

      HashSet<ulong> models = new HashSet<ulong>();
      HashSet<ulong> parsed = new HashSet<ulong>();
      Dictionary<ulong, Stream> textures = new Dictionary<ulong, Stream>();
      Dictionary<ulong, ImageLayer> layers = new Dictionary<ulong, ImageLayer>();

      FindModels(master.Header.binding.key, models, textures, layers, parsed, map, handler);

      // iterate layers -> save textures
      // iterate models -> save models w/ layers.
    }
  }
}
