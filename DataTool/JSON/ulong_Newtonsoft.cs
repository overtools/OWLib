using System;
using Newtonsoft.Json;

namespace DataTool.JSON {
    public class ulong_Newtonsoft : JsonConverter<ulong> {
        public override void WriteJson(JsonWriter writer, ulong value, JsonSerializer serializer) {
            writer.WriteValue(value.ToString("X16"));
        }

        public override ulong ReadJson(JsonReader reader, Type objectType, ulong existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }
    }
}
