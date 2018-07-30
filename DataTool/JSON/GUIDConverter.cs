using System;
using Newtonsoft.Json;
using TankLib;

namespace DataTool.JSON {
    public class GUIDConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteStartObject();

            writer.WritePropertyName("Key");
            ulong key;
            if (value is teResourceGUID resourceGUID) {
                key = resourceGUID;
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
            return typeof(teResourceGUID).IsAssignableFrom(objectType) || typeof(ulong).IsAssignableFrom(objectType);
        }
    }
    
    public class GUIDArrayConverter : GUIDConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {          
            ulong[] keys;
            if (value is teResourceGUID[] resourceGUIDs) {
                keys = new ulong[resourceGUIDs.Length];
                for (int i = 0; i < resourceGUIDs.Length; i++) {
                    keys[i] = resourceGUIDs[i];
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
            return typeof(teResourceGUID[]).IsAssignableFrom(objectType) || typeof(ulong[]).IsAssignableFrom(objectType);
        }
    }
}
