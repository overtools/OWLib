using System;
using Newtonsoft.Json;
using TankLib;

namespace DataTool.JSON {
    public class teResourceGUID_Newtonsoft : JsonConverter<teResourceGUID> {
        public override void WriteJson(JsonWriter writer, teResourceGUID value, JsonSerializer serializer) {
            writer.WriteStartObject();
            writer.WritePropertyName(value.GUID.ToString("X8"));
            writer.WriteValue(value.ToString());
            writer.WriteEndObject();
        }

        public override teResourceGUID ReadJson(JsonReader reader, Type objectType, teResourceGUID existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }
    }
}
