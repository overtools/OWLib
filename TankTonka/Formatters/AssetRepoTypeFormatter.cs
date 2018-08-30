using Utf8Json;
using static TankTonka.Models.Common;

namespace TankTonka.Formatters {
    public class AssetRepoTypeFormatter : IJsonFormatter<AssetRepoType> {
        public void Serialize(ref JsonWriter writer, AssetRepoType value, IJsonFormatterResolver formatterResolver) {
            FourCCFormatter.SerializeUint(ref writer, (ushort)value, formatterResolver, "X3");
        }

        public AssetRepoType Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver) {
            return (AssetRepoType)FourCCFormatter.DeserializeUint(ref reader, formatterResolver);
        }
    }
}