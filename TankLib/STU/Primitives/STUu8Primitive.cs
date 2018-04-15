using System;

namespace TankLib.STU.Primitives {
    /// <summary>Byte\/byte STU primitive</summary>
    public class STUu8Primitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            return data.Data.ReadByte();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            return data.DynData.ReadByte();
        }

        public Type GetValueType() {
            return typeof(byte);
        }
    }
}