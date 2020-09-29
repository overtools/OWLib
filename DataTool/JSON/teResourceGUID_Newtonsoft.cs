using System;
using Newtonsoft.Json;
using TankLib;

namespace DataTool.JSON {
    public class teResourceGUID_Newtonsoft : JsonConverter<teResourceGUID> {
        public override void WriteJson(JsonWriter writer, teResourceGUID value, JsonSerializer serializer) {
            writer.WriteValue(value.ToString());
        }

        public override teResourceGUID ReadJson(JsonReader reader, Type objectType, teResourceGUID existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }
    }
}
