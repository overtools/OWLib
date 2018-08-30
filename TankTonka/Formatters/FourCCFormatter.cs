using System.Globalization;
using Utf8Json;

namespace TankTonka.Formatters {
    public class FourCCFormatter : IJsonFormatter<uint> {
        public void Serialize(ref JsonWriter writer, uint value, IJsonFormatterResolver formatterResolver) {
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.ToString("X8"), formatterResolver);
        }

        public uint Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver) {
            return uint.Parse(formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver), NumberStyles.HexNumber);
        }
    }

    public class FourCCArrayFormatter : IJsonFormatter<uint[]> {
        public void Serialize(ref JsonWriter writer, uint[] value, IJsonFormatterResolver formatterResolver) {
            if (value == null) {
                writer.WriteNull();
                return;
            }
            
            writer.WriteBeginArray();
            
            FourCCFormatter formatter = new FourCCFormatter();

            foreach (uint val in value) {
                formatter.Serialize(ref writer, val, formatterResolver);
            }
            
            writer.WriteEndArray();
        }

        public uint[] Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver) {
            throw new System.NotImplementedException();
        }
    }
}