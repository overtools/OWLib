using System;

namespace TankLib.STU.Primitives {
    /// <summary>Double/double STU primitive</summary>
    public class STUf64Primitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            return data.Data.ReadDouble();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            return data.DynData.ReadDouble();
        }

        public Type GetValueType() {
            return typeof(double);
        }
    }
}