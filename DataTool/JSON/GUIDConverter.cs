using System;
using System.Linq;
using Newtonsoft.Json;
using OWLib;
using TankLib;
using static STULib.Types.Generic.Common;

namespace DataTool.JSON {
    public class GUIDConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteStartObject();

            writer.WritePropertyName("Key");
            ulong key;
            if (value is teResourceGUID resourceGUID) {
                key = resourceGUID;
            } else if (value is STUGUID guid) {
                key = guid;
            } else {
                key = (ulong)value;
            }
            writer.WriteValue($"{key:X16}");

            writer.WritePropertyName("String");
            writer.WriteValue(teResourceGUID.AsString(key));

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            return 0;
        }

        public override bool CanConvert(Type objectType) {
            return typeof(STUGUID).IsAssignableFrom(objectType) || typeof(ulong).IsAssignableFrom(objectType);
        }
    }
    
    public class GUIDArrayConverter : GUIDConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {          
            ulong[] keys;
            if (value is STUGUID[] guid) {
                keys = new ulong[guid.Length];
                for (int i = 0; i < guid.Length; i++) {
                    keys[i] = guid[i];
                }
            } else {
                keys = (ulong[])value;
            }

            writer.WriteStartArray();
            foreach (ulong key in keys) {
                writer.WriteStartObject();
                writer.WritePropertyName("Key");
                writer.WriteValue($"{key:X16}");
                writer.WritePropertyName("String");
                writer.WriteValue(teResourceGUID.AsString(key));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        public override bool CanConvert(Type objectType) {
            return typeof(STUGUID[]).IsAssignableFrom(objectType) || typeof(ulong[]).IsAssignableFrom(objectType);
        }
    }
}
