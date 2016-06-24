using System;
using System.Collections.Generic;
using System.IO;
using OWLib.Types.Map;
using OWLib.Types;

namespace OWLib.ModelWriter {
  public class OWMAPWriter {
    public string Format => ".owmap";

    public void Write(Stream output, Map map) {
      using(BinaryWriter writer = new BinaryWriter(output)) {
        writer.Write((ushort)1); // version major
        writer.Write((ushort)0); // version minor

        uint size = 0;

        for(int i = 0; i < map.Records.Length; ++i) {
          Map01 obj = (Map01)map.Records[i];
          if(obj == null) {
            continue;
          }
          size++;
        }
        writer.Write(size); // nr objects

        for(int i = 0; i < map.Records.Length; ++i) {
          Map01 obj = (Map01)map.Records[i];
          if(obj == null) {
            continue;
          }
          writer.Write(string.Format("{0:X12}.owmdl", APM.keyToIndexID(obj.Header.model)));
          writer.Write(obj.Header.groupCount);
          for(int j = 0; j < obj.Header.groupCount; ++j) {
            Map01.Map01Group group = obj.Groups[j];
            writer.Write(string.Format("{0:X12}_{1:X12}.owmat", APM.keyToIndexID(obj.Header.model), APM.keyToIndexID(group.material)));
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
      }
    }
  }
}
