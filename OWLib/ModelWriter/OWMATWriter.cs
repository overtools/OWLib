using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OWLib.Types;
using OWLib.Types.Map;

namespace OWLib.ModelWriter {
  public class OWMATWriter : IModelWriter {
    public string Format => ".owmat";

    public char[] Identifier => new char[1] { 'W' };
    public string Name => "OWM Material Format";

    public ModelWriterSupport SupportLevel => ModelWriterSupport.MATERIAL;

    public bool Write(Map10 physics, Stream output, object[] data) {
      return false;
    }

    public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
      Console.Out.WriteLine("Writing OWMAT");
      ushort versionMajor = 1;
      ushort versionMinor = 0;

      bool hasTypeData = false;
      if(data != null && data.Length > 0 && data[0].GetType() == typeof(List<TextureType>)) {
        hasTypeData = true;
        versionMajor = 1;
        versionMinor = 1;
      }

      using(BinaryWriter writer = new BinaryWriter(output)) {
        writer.Write(versionMajor);
        writer.Write(versionMinor);
        writer.Write(layers.Keys.LongCount()); // nr materials

        foreach(KeyValuePair<ulong, List<ImageLayer>> layer in layers) {
          writer.Write(layer.Key);
          HashSet<string> images = new HashSet<string>();
          foreach(ImageLayer image in layer.Value) {
            images.Add(string.Format("{0:X12}.dds", APM.keyToIndexID(image.key)));
          }
          writer.Write(images.Count);
          foreach(string image in images) {
            writer.Write(image);
          }
          if(hasTypeData) {
            foreach(TextureType @type in (List<TextureType>)data[0]) {
              writer.Write((byte)DDSTypeDetect.Detect(@type));
            }
          }
        }
      }
      return true;
    }
  }
}