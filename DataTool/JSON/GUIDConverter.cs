using System;
using Newtonsoft.Json;
using OWLib;
using static STULib.Types.Generic.Common;

namespace DataTool.JSON {
    public class GUIDConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteStartObject();

            writer.WritePropertyName("Key");
            ulong key = 0;
            STUGUID guid = value as STUGUID;
            if (guid != null) {
                key = guid;
            } else {
                key = (ulong)value;
            }
            writer.WriteValue($"{key:X16}");

            writer.WritePropertyName("String");
            writer.WriteValue($"{GUID.LongKey(key):X12}.{GUID.Type(key):X3}");

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            return 0;
        }

        public override bool CanConvert(Type objectType) {
            return typeof(STUGUID).IsAssignableFrom(objectType) || typeof(ulong).IsAssignableFrom(objectType);
        }
    }
}
