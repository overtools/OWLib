using TankLib;
using Utf8Json;

namespace TankTonka.Formatters {
    public class ResourceGUIDFormatter : IJsonFormatter<teResourceGUID> {
        public void Serialize(ref JsonWriter writer, teResourceGUID value, IJsonFormatterResolver formatterResolver) {
            if (value == 0) { writer.WriteNull(); return; }

            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, teResourceGUID.AsString(value), formatterResolver);
        }

        teResourceGUID IJsonFormatter<teResourceGUID>.Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver) {
            throw new System.NotImplementedException();
        }
    }
}