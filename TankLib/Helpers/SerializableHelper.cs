using System.IO;

namespace TankLib.Helpers {
    public class SerializableHelper {
        public static void Deserialize<T>(BinaryReader reader, out T val) where T : ISerializable, new() {
            val = new T();
            val.Deserialize(reader);
        }
    }
}