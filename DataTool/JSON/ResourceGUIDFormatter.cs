using TankLib;
using Utf8Json;

namespace DataTool.JSON {
    public class ResourceGUIDFormatter : IJsonFormatter<teResourceGUID>, IJsonFormatter<teResourceGUID[]> {
        public void Serialize(ref JsonWriter writer, teResourceGUID value, IJsonFormatterResolver formatterResolver) {
            Serialize(ref writer, (ulong)value, formatterResolver);
        }

        public void Serialize(ref JsonWriter writer, ulong value, IJsonFormatterResolver formatterResolver) {
            if (value == 0) { writer.WriteNull(); return; }

            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, teResourceGUID.AsString(value), formatterResolver);
        }

        public void Serialize(ref JsonWriter writer, teResourceGUID[] value, IJsonFormatterResolver formatterResolver) {
            if (value == null) { writer.WriteNull(); return; }
            
            writer.WriteBeginArray();
            
            if (value.Length != 0)
            {
                Serialize(ref writer, value[0], formatterResolver);
            }
            for (int i = 1; i < value.Length; i++)
            {
                writer.WriteValueSeparator(); // write "," manually
                Serialize(ref writer, value[i], formatterResolver);
            }
            
            writer.WriteEndArray();
        }

        teResourceGUID[] IJsonFormatter<teResourceGUID[]>.Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver) {
            throw new System.NotImplementedException();
        }

        public ulong Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver) {
            throw new System.NotImplementedException();
        }

        teResourceGUID IJsonFormatter<teResourceGUID>.Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver) {
            throw new System.NotImplementedException();
        }
    }
}
