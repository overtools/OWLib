using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OWLib.Types;
using OWLib.Types.Map;

namespace OWLib.Writer {
    public class MTLWriter : IDataWriter {
        public string Name => "Wavefront MTL";
        public string Format => ".mtl";
        public char[] Identifier => new char[1] { 'O' };
        public WriterSupport SupportLevel => WriterSupport.MATERIAL | WriterSupport.MATERIAL_DEF;

        public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
            
            Dictionary<string, TextureType> typeData = null;
            if (data != null && data.Length > 0 && data[0].GetType() == typeof(Dictionary<string, TextureType>)) {
                typeData = (Dictionary<string, TextureType>)data[0];
            }

            Dictionary<ulong, Dictionary<ulong, string>> nameMap = new Dictionary<ulong, Dictionary<ulong, string>>();
            foreach (KeyValuePair<ulong, List<ImageLayer>> layer in layers) {
                nameMap[layer.Key] = new Dictionary<ulong, string>();
                foreach (ImageLayer image in layer.Value) {
                    string old = $"{GUID.LongKey(image.Key):X12}.dds";
                    if (typeData != null) {
                        try {
                            nameMap[layer.Key].Add(image.Key, typeData.First(new Func<KeyValuePair<string, TextureType>, bool>(delegate (KeyValuePair<string, TextureType> input) {
                                return Path.GetFileName(input.Key).ToUpperInvariant() == old.ToUpperInvariant();
                            })).Key);
                        } catch {
                            nameMap[layer.Key].Add(image.Key, old);
                        }
                    } else {
                        nameMap[layer.Key].Add(image.Key, old);
                    }
                }
            }

            using (StreamWriter writer = new StreamWriter(output)) {
                foreach (KeyValuePair<ulong, List<ImageLayer>> pair in layers) {
                    writer.WriteLine("newmtl {0:X16}", pair.Key);
                    writer.WriteLine("Kd 1 1 1");

                    foreach (ImageLayer layer in pair.Value) {
                        writer.WriteLine("map_Kd \"{0}\"", nameMap[pair.Key][layer.Key]);
                    }
                    writer.WriteLine("");
                }
            }
            return true;
        }

        public bool Write(Map10 physics, Stream output, object[] data) {
            return false;
        }

        public bool Write(Animation anim, Stream output, object[] data) {
            return false;
        }

        public Dictionary<ulong, List<string>>[] Write(Stream output, Map map, Map detail1, Map detail2, Map props, Map lights, string name, IDataWriter modelFormat) {
            return null;
        }
    }
}