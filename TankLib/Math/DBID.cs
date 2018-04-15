using TankLib.STU;

namespace TankLib.Math {
    // ReSharper disable once InconsistentNaming
    public class DBID : ISerializable_STU {
        public byte[] Value;
        
        public void Deserialize(teStructuredData data, STUField_Info field) {
            Value = data.Data.ReadBytes(16);
        }

        public void Deserialize_Array(teStructuredData data, STUField_Info field) {
            Value = data.DynData.ReadBytes(16);
        }
    }
}