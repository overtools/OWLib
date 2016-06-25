using System;
using System.Collections.Generic;
using System.IO;
using OWLib.Types.Map;
using OWLib.Types;

namespace OWLib.ModelWriter {
  public class OWMAPWriter {
    public string Format => ".owmap";

    public Dictionary<ulong, List<string>>[] Write(Stream output, Map map, string name = "") {
      using(BinaryWriter writer = new BinaryWriter(output)) {
        writer.Write((ushort)1); // version major
        writer.Write((ushort)0); // version minor

        if(name.Length == 0) {
          writer.Write((byte)0);
        } else {
          writer.Write(name);
        }
        uint size = 0;

        for(int i = 0; i < map.Records.Length; ++i) {
          Map01 obj = (Map01)map.Records[i];
          if(obj == null) {
            continue;
          }
          size++;
        }
        writer.Write(size); // nr objects

        Dictionary<ulong, List<string>>[] ret = new Dictionary<ulong, List<string>>[2];
        ret[0] = new Dictionary<ulong, List<string>>();
        ret[1] = new Dictionary<ulong, List<string>>();

        for(int i = 0; i < map.Records.Length; ++i) {
          Map01 obj = (Map01)map.Records[i];
          if(obj == null) {
            continue;
          }
          string modelFn = string.Format("{0:X12}.owmdl", APM.keyToIndexID(obj.Header.model));
          writer.Write(modelFn);
          if(!ret[0].ContainsKey(obj.Header.model)) {
            ret[0].Add(obj.Header.model, new List<string>());
          }
          ret[0][obj.Header.model].Add(modelFn);
          writer.Write(obj.Header.groupCount);
          for(int j = 0; j < obj.Header.groupCount; ++j) {
            Map01.Map01Group group = obj.Groups[j];
            string materialFn = string.Format("{0:X12}_{1:X12}.owmat", APM.keyToIndexID(obj.Header.model), APM.keyToIndexID(group.material));
            writer.Write(materialFn);
            if(!ret[1].ContainsKey(group.material)) {
              ret[1].Add(group.material, new List<string>());
            }
            ret[1][group.material].Add(materialFn);
            writer.Write(group.recordCount);
            for(int k = 0; k < group.recordCount; ++k) {
              Map01.Map01GroupRecord record = obj.Records[j][k];
              writer.Write(record.position.x);
              writer.Write(record.position.y);
              writer.Write(record.position.z);
              writer.Write(record.scale.x);
              writer.Write(record.scale.y);
              writer.Write(record.scale.z);
              writer.Write(record.rotation.x);
              writer.Write(record.rotation.y);
              writer.Write(record.rotation.z);
              writer.Write(record.rotation.w);
            }
          }
        }
        return ret;
      }
    }
  }
}
