using System;
using Newtonsoft.Json;

namespace DataTool.JSON {
    public class long_Newtonsoft : JsonConverter<long> {
        public override void WriteJson(JsonWriter writer, long value, JsonSerializer serializer) {
            writer.WriteValue(value.ToString("X16"));
        }

        public override long ReadJson(JsonReader reader, Type objectType, long existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }
    }
}
