using System.Globalization;
using TankTonka.Models;
using Utf8Json;

namespace TankTonka.Formatters {
    public class AssetRepoTypeFormatter : IJsonFormatter<Common.AssetRepoType> {
        public void Serialize(ref JsonWriter writer, Common.AssetRepoType value, IJsonFormatterResolver formatterResolver) {
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, ((ushort)value).ToString("X3"), formatterResolver);
        }

        public Common.AssetRepoType Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver) {
            return (Common.AssetRepoType)ushort.Parse("0x"+formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver), NumberStyles.HexNumber);
        }
    }
}