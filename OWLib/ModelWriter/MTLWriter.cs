using System;
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

        public void Write(Model model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] opts) {
            using(StreamWriter writer = new StreamWriter(output)) {
                foreach(KeyValuePair<ulong, List<ImageLayer>> pair in layers) {
                    writer.WriteLine("newmtl {0:X16}", pair.Key);
                    writer.WriteLine("Kd 1 1 1");

                    foreach(ImageLayer layer in pair.Value) {
                        writer.WriteLine("map_Kd \"{0:X12}.dds\"", APM.keyToIndexID(layer.key));
                    }
                    writer.WriteLine("");
                }
            }
        }

        public void Write(Map10 physics, Stream output, object[] data) {
        }
    }
}
