using System;

namespace TankLib.STU.Primitives {
    /// <summary>Single/float STU primitive</summary>
    public class STUf32Primitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            return data.Data.ReadSingle();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            return data.DynData.ReadSingle();
        }

        public Type GetValueType() {
            return typeof(float);
        }
    }
}