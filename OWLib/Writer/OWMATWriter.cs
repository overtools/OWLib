using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OWLib.Types;
using OWLib.Types.Map;

namespace OWLib.Writer {
    public class OWMATWriter : IDataWriter {
        public string Format => ".owmat";
        public char[] Identifier => new char[1] { 'W' };
        public string Name => "OWM Material Format";
        public WriterSupport SupportLevel => WriterSupport.MATERIAL | WriterSupport.MATERIAL_DEF;

        public bool Write(Map10 physics, Stream output, object[] data) {
            return false;
        }

        public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
            //Console.Out.WriteLine("Writing OWMAT");
            ushort versionMajor = 1;
            ushort versionMinor = 0;

            Dictionary<string, TextureType> typeData = null;
            if (data != null && data.Length > 0 && data[0].GetType() == typeof(Dictionary<string, TextureType>)) {
                typeData = (Dictionary<string, TextureType>)data[0];
                versionMajor = 1;
                versionMinor = 1;
            }

            using (BinaryWriter writer = new BinaryWriter(output)) {
                writer.Write(versionMajor);
                writer.Write(versionMinor);
                writer.Write(layers.Keys.LongCount()); // nr materials

                foreach (KeyValuePair<ulong, List<ImageLayer>> layer in layers) {
                    writer.Write(layer.Key);
                    HashSet<string> images = new HashSet<string>();
                    foreach (ImageLayer image in layer.Value) {
                        string old = $"{GUID.LongKey(image.Key):X12}.dds";
                        if (typeData != null) {
                            try {
                                images.Add(typeData.First(new Func<KeyValuePair<string, TextureType>, bool>(delegate (KeyValuePair<string, TextureType> input) {
                                    return Path.GetFileName(input.Key).ToUpperInvariant() == old.ToUpperInvariant();
                                })).Key);
                            } catch {
                                images.Add(old);
                            }
                        } else {
                            images.Add(old);
                        }
                    }
                    writer.Write(images.Count);
                    foreach (string image in images) {
                        writer.Write(image);
                        if (typeData != null && typeData.ContainsKey(image)) {
                            writer.Write((byte)DDSTypeDetect.Detect(typeData[image]));
                        } else {
                            writer.Write((byte)0xFF);
                        }
                    }
                }
            }
            return true;
        }

        public bool Write(Animation anim, Stream output, object[] data) {
            return false;
        }

        public Dictionary<ulong, List<string>>[] Write(Stream output, Map map, Map detail1, Map detail2, Map props, Map lights, string name, IDataWriter modelFormat) {
            return null;
        }
    }
}
