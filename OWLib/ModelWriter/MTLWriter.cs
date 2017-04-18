using System.Collections.Generic;
using System.IO;
using OWLib.Types;
using OWLib.Types.Map;

namespace OWLib.ModelWriter {
    public class MTLWriter : IModelWriter {
        public string Name => "Wavefront MTL";
        public string Format => ".mtl";
        public char[] Identifier => new char[1] { 'O' };

        public ModelWriterSupport SupportLevel => ModelWriterSupport.MATERIAL;

        public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] opts) {
            using (StreamWriter writer = new StreamWriter(output)) {
                foreach (KeyValuePair<ulong, List<ImageLayer>> pair in layers) {
                    writer.WriteLine("newmtl {0:X16}", pair.Key);
                    writer.WriteLine("Kd 1 1 1");

                    foreach (ImageLayer layer in pair.Value) {
                        writer.WriteLine("map_Kd \"{0:X12}.dds\"", GUID.Attribute(layer.key, GUID.AttributeEnum.Index | GUID.AttributeEnum.Locale | GUID.AttributeEnum.Region | GUID.AttributeEnum.Platform));
                    }
                    writer.WriteLine("");
                }
            }
            return true;
        }

        public bool Write(Map10 physics, Stream output, object[] data) {
            return false;
        }
    }
}