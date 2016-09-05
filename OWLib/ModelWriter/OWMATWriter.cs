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

        public void Write(Map10 physics, Stream output, object[] data) {
        }

        public void Write(Model model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
            Console.Out.WriteLine("Writing OWMAT");
            using(BinaryWriter writer = new BinaryWriter(output)) {
                writer.Write((ushort)1); // version major
                writer.Write((ushort)0); // version minor
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
                }
            }
        }
    }
}
