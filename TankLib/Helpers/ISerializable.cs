using System.IO;

namespace TankLib.Helpers {
    public interface ISerializable {
        void Deserialize(BinaryReader reader);
        void Serialize(BinaryWriter writer);
    }
}