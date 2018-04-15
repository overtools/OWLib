using System;

namespace TankLib.STU.Primitives {
    /// <summary>Int64/long STU primitive</summary>
    public class STUs64Primitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            return data.Data.ReadInt64();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            return data.DynData.ReadInt64();
        }

        public Type GetValueType() {
            return typeof(long);
        }
    }
}