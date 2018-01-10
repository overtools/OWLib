using System.Collections.Generic;
using System.IO;
using OWLib.Types;
using OWLib.Types.Map;

namespace OWLib.Writer {
    public class OWMAPWriter : IDataWriter {
        public string Format => ".owmap";
        public WriterSupport SupportLevel => WriterSupport.VERTEX | WriterSupport.MAP;
        public char[] Identifier => new char[1] { 'O' };
        public string Name => "OWM Map Format";

        public bool Write(Animation anim, Stream output, object[] data) {
            return false;
        }

        public bool Write(Map10 physics, Stream output, object[] data) {
            return false;
        }

        public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
            return false;
        }

        public Dictionary<ulong, List<string>>[] Write(Stream output, Map map, Map detail1, Map detail2, Map props, Map lights, string name, IDataWriter modelFormat) {
            if (modelFormat == null) {
                modelFormat = new OWMDLWriter();
            }
            //Console.Out.WriteLine("Writing OWMAP");
            using (BinaryWriter writer = new BinaryWriter(output)) {
                writer.Write((ushort)1); // version major
                writer.Write((ushort)1); // version minor

                if (name.Length == 0) {
                    writer.Write((byte)0);
                } else {
                    writer.Write(name);
                }

                uint size = 0;
                for (int i = 0; i < map.Records.Length; ++i) {
                    if (map.Records[i] != null && map.Records[i].GetType() != typeof(Map01)) {
                        continue;
                    }
                    size++;
                }
                writer.Write(size); // nr objects

                size = 1;
                for (int i = 0; i < detail1.Records.Length; ++i) {
                    if (detail1.Records[i] != null && detail1.Records[i].GetType() != typeof(Map02)) {
                        continue;
                    }
                    size++;
                }
                for (int i = 0; i < detail2.Records.Length; ++i) {
                    if (detail2.Records[i] != null && detail2.Records[i].GetType() != typeof(Map08)) {
                        continue;
                    }
                    size++;
                }
                for (int i = 0; i < props.Records.Length; ++i) {
                    if (props.Records[i] != null && props.Records[i].GetType() != typeof(Map0B)) {
                        continue;
                    }
                    if (((Map0B)props.Records[i]).Model == 0) {
                        continue;
                    }
                    size++;
                }
                writer.Write(size); // nr details

                // Extension 1.1 - Lights
                size = 0;
                for (int i = 0; i < lights.Records.Length; ++i) {
                    if (lights.Records[i] != null && lights.Records[i].GetType() != typeof(Map09)) {
                        continue;
                    }
                    size++;
                }
                writer.Write(size); // nr Lights


                Dictionary<ulong, List<string>>[] ret = new Dictionary<ulong, List<string>>[2];
                ret[0] = new Dictionary<ulong, List<string>>();
                ret[1] = new Dictionary<ulong, List<string>>();

                for (int i = 0; i < map.Records.Length; ++i) {
                    if (map.Records[i] != null && map.Records[i].GetType() != typeof(Map01)) {
                        continue;
                    }
                    Map01 obj = (Map01)map.Records[i];
                    string modelFn = $"{GUID.Index(obj.Header.Model):X12}{modelFormat.Format}";
                    writer.Write(modelFn);
                    if (!ret[0].ContainsKey(obj.Header.Model)) {
                        ret[0].Add(obj.Header.Model, new List<string>());
                    }
                    ret[0][obj.Header.Model].Add(modelFn);
                    writer.Write(obj.Header.groupCount);
                    for (int j = 0; j < obj.Header.groupCount; ++j) {
                        Map01.Map01Group group = obj.Groups[j];
                        string materialFn =
                            $"{GUID.Index(obj.Header.Model):X12}_{GUID.Index(group.ModelLook):X12}.owmat";
                        writer.Write(materialFn);
                        if (!ret[1].ContainsKey(group.ModelLook)) {
                            ret[1].Add(group.ModelLook, new List<string>());
                        }
                        ret[1][group.ModelLook].Add(materialFn);
                        writer.Write(group.recordCount);
                        for (int k = 0; k < group.recordCount; ++k) {
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

                writer.Write($"physics.{modelFormat.Format}");
                writer.Write((byte)0);
                writer.Write(0.0f);
                writer.Write(0.0f);
                writer.Write(0.0f);
                writer.Write(1.0f);
                writer.Write(1.0f);
                writer.Write(1.0f);
                writer.Write(0.0f);
                writer.Write(0.0f);
                writer.Write(0.0f);
                writer.Write(1.0f);

                for (int i = 0; i < detail1.Records.Length; ++i) {
                    if (detail1.Records[i] != null && detail1.Records[i].GetType() != typeof(Map02)) {
                        continue;
                    }
                    Map02 obj = (Map02)detail1.Records[i];
                    string modelFn = $"{GUID.LongKey(obj.Header.Model):X12}{modelFormat.Format}";
                    string matFn = $"{GUID.LongKey(obj.Header.Model):X12}.owmat";
                    writer.Write(modelFn);
                    writer.Write(matFn);
                    writer.Write(obj.Header.position.x);
                    writer.Write(obj.Header.position.y);
                    writer.Write(obj.Header.position.z);
                    writer.Write(obj.Header.scale.x);
                    writer.Write(obj.Header.scale.y);
                    writer.Write(obj.Header.scale.z);
                    writer.Write(obj.Header.rotation.x);
                    writer.Write(obj.Header.rotation.y);
                    writer.Write(obj.Header.rotation.z);
                    writer.Write(obj.Header.rotation.w);

                    if (!ret[0].ContainsKey(obj.Header.Model)) {
                        ret[0].Add(obj.Header.Model, new List<string>());
                    }
                    ret[0][obj.Header.Model].Add(modelFn);


                    if (!ret[1].ContainsKey(obj.Header.ModelLook)) {
                        ret[1].Add(obj.Header.ModelLook, new List<string>());
                    }
                    ret[1][obj.Header.ModelLook].Add(matFn);
                }

                for (int i = 0; i < detail2.Records.Length; ++i) {
                    if (detail2.Records[i] != null && detail2.Records[i].GetType() != typeof(Map08)) {
                        continue;
                    }
                    Map08 obj = (Map08)detail2.Records[i];
                    string modelFn = $"{GUID.LongKey(obj.Header.Model):X12}{modelFormat.Format}";
                    string matFn = $"{GUID.LongKey(obj.Header.Model):X12}.owmat";
                    writer.Write(modelFn);
                    writer.Write(matFn);
                    writer.Write(obj.Header.position.x);
                    writer.Write(obj.Header.position.y);
                    writer.Write(obj.Header.position.z);
                    writer.Write(obj.Header.scale.x);
                    writer.Write(obj.Header.scale.y);
                    writer.Write(obj.Header.scale.z);
                    writer.Write(obj.Header.rotation.x);
                    writer.Write(obj.Header.rotation.y);
                    writer.Write(obj.Header.rotation.z);
                    writer.Write(obj.Header.rotation.w);

                    if (!ret[0].ContainsKey(obj.Header.Model)) {
                        ret[0].Add(obj.Header.Model, new List<string>());
                    }
                    ret[0][obj.Header.Model].Add(modelFn);


                    if (!ret[1].ContainsKey(obj.Header.ModelLook)) {
                        ret[1].Add(obj.Header.ModelLook, new List<string>());
                    }
                    ret[1][obj.Header.ModelLook].Add(matFn);
                }

                for (int i = 0; i < props.Records.Length; ++i) {
                    if (props.Records[i] != null && props.Records[i].GetType() != typeof(Map0B)) {
                        continue;
                    }
                    Map0B obj = (Map0B)props.Records[i];
                    if (obj.Model == 0) {
                        continue;
                    }
                    string modelFn = $"{GUID.LongKey(obj.Model):X12}{modelFormat.Format}";
                    string matFn = $"{GUID.LongKey(obj.ModelLook):X12}.owmat";
                    writer.Write(modelFn);
                    writer.Write(matFn);
                    writer.Write(obj.Header.position.x);
                    writer.Write(obj.Header.position.y);
                    writer.Write(obj.Header.position.z);
                    writer.Write(obj.Header.scale.x);
                    writer.Write(obj.Header.scale.y);
                    writer.Write(obj.Header.scale.z);
                    writer.Write(obj.Header.rotation.x);
                    writer.Write(obj.Header.rotation.y);
                    writer.Write(obj.Header.rotation.z);
                    writer.Write(obj.Header.rotation.w);

                    if (!ret[0].ContainsKey(obj.Model)) {
                        ret[0].Add(obj.Model, new List<string>());
                    }
                    ret[0][obj.Model].Add(modelFn);


                    if (!ret[1].ContainsKey(obj.ModelLook)) {
                        ret[1].Add(obj.ModelLook, new List<string>());
                    }
                    ret[1][obj.ModelLook].Add(matFn);
                }

                // Extension 1.1 - Lights
                for (int i = 0; i < lights.Records.Length; ++i) {
                    if (lights.Records[i] != null && lights.Records[i].GetType() != typeof(Map09)) {
                        continue;
                    }
                    Map09 obj = (Map09)lights.Records[i];
                    writer.Write(obj.Header.position.x);
                    writer.Write(obj.Header.position.y);
                    writer.Write(obj.Header.position.z);
                    writer.Write(obj.Header.rotation.x);
                    writer.Write(obj.Header.rotation.y);
                    writer.Write(obj.Header.rotation.z);
                    writer.Write(obj.Header.rotation.w);
                    writer.Write(obj.Header.LightType);
                    writer.Write(obj.Header.LightFOV);
                    writer.Write(obj.Header.Color.x);
                    writer.Write(obj.Header.Color.y);
                    writer.Write(obj.Header.Color.z);
                    writer.Write(obj.Header.unknown1A);
                    writer.Write(obj.Header.unknown1B);
                    writer.Write(obj.Header.unknown2A);
                    writer.Write(obj.Header.unknown2B);
                    writer.Write(obj.Header.unknown2C);
                    writer.Write(obj.Header.unknown2D);
                    writer.Write(obj.Header.unknown3A);
                    writer.Write(obj.Header.unknown3B);

                    writer.Write(obj.Header.unknownPos1.x);
                    writer.Write(obj.Header.unknownPos1.y);
                    writer.Write(obj.Header.unknownPos1.z);
                    writer.Write(obj.Header.unknownQuat1.x);
                    writer.Write(obj.Header.unknownQuat1.y);
                    writer.Write(obj.Header.unknownQuat1.z);
                    writer.Write(obj.Header.unknownQuat1.w);
                    writer.Write(obj.Header.unknownPos2.x);
                    writer.Write(obj.Header.unknownPos2.y);
                    writer.Write(obj.Header.unknownPos2.z);
                    writer.Write(obj.Header.unknownQuat2.x);
                    writer.Write(obj.Header.unknownQuat2.y);
                    writer.Write(obj.Header.unknownQuat2.z);
                    writer.Write(obj.Header.unknownQuat2.w);
                    writer.Write(obj.Header.unknownPos3.x);
                    writer.Write(obj.Header.unknownPos3.y);
                    writer.Write(obj.Header.unknownPos3.z);
                    writer.Write(obj.Header.unknownQuat3.x);
                    writer.Write(obj.Header.unknownQuat3.y);
                    writer.Write(obj.Header.unknownQuat3.z);
                    writer.Write(obj.Header.unknownQuat3.w);

                    writer.Write(obj.Header.unknown4A);
                    writer.Write(obj.Header.unknown4B);
                    writer.Write(obj.Header.unknown5);
                    writer.Write(obj.Header.unknown6A);
                    writer.Write(obj.Header.unknown6B);
                    writer.Write(obj.Header.unknown7A);
                    writer.Write(obj.Header.unknown7B);
                }

                return ret;
            }
        }
    }
}
