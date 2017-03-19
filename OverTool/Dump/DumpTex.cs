using System;
using System.Collections.Generic;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD.Binding;

namespace OverTool {
  class DumpTex : IOvertool {
    public string Help => "<model ids...>";
    public uint MinimumArgs => 1;
    public char Opt => 'T';
    public string Title => "List Textures";
    public ushort[] Track => new ushort[1] { 0x3 };

    public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
      List<ulong> ids = new List<ulong>();
      foreach(string arg in args) {
        ids.Add(ulong.Parse(arg.Split('.')[0], System.Globalization.NumberStyles.HexNumber));
      }
      Console.Out.WriteLine("Scanning for textures...");
      foreach(ulong f003 in track[0x3]) {
        STUD record = new STUD(Util.OpenFile(map[f003], handler), true, STUDManager.Instance, false, true);
        if(record.Instances == null) {
          continue;
        }
        foreach(ISTUDInstance instance in record.Instances) {
          if(instance == null) {
            continue;
          }
          if(instance.Name == record.Manager.GetName(typeof(ComplexModelRecord))) {
            ComplexModelRecord r = (ComplexModelRecord)instance;
            if(ids.Contains(GUID.Attribute(r.Data.model.key, GUID.AttributeEnum.Index | GUID.AttributeEnum.Locale | GUID.AttributeEnum.Region | GUID.AttributeEnum.Platform))) {
              Dictionary<ulong, List<ImageLayer>> layers = new Dictionary<ulong, List<ImageLayer>>();
              ExtractLogic.Skin.FindTextures(r.Data.material.key, layers, new Dictionary<ulong, ulong>(), new HashSet<ulong>(), map, handler);
              Console.Out.WriteLine("Model ID {0:X12}", GUID.Attribute(r.Data.model.key, GUID.AttributeEnum.Index | GUID.AttributeEnum.Locale | GUID.AttributeEnum.Region | GUID.AttributeEnum.Platform));
              foreach(KeyValuePair<ulong, List<ImageLayer>> pair in layers) {
                Console.Out.WriteLine("Material ID {0:X16}", pair.Key);
                HashSet<ulong> dedup = new HashSet<ulong>();
                foreach(ImageLayer layer in pair.Value) {
                  if(dedup.Add(layer.key)) {
                    Console.Out.WriteLine("Texture ID {0:X12}", GUID.Attribute(layer.key, GUID.AttributeEnum.Index | GUID.AttributeEnum.Locale | GUID.AttributeEnum.Region | GUID.AttributeEnum.Platform));
                  }
                }
              }
              ids.Remove(GUID.Attribute(r.Data.model.key, GUID.AttributeEnum.Index | GUID.AttributeEnum.Locale | GUID.AttributeEnum.Region | GUID.AttributeEnum.Platform));
            }
          }
        }
      }
    }
  }
}
